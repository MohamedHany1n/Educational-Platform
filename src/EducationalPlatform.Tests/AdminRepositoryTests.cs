using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EducationalPlatform.Tests;

/// <summary>
/// Regression tests for the admin payment-review and phone-uniqueness fixes.
/// These run on SQLite in memory and cover the sequential business logic.
/// Concurrent-request races live in PaymentConcurrencyTests. The SQL Server
/// error-number translation (2601/2627 in IsUniqueViolation) is covered by
/// reasoning only; SQLite's constraint error exercises the same branch shape.
/// </summary>
public class AdminRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly AdminRepository _repository;

    public AdminRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new AdminRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private ApplicationUser SeedUser(string email, string? phone)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = email,
            PhoneNumber = phone,
            IsActive = true
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    private Course SeedCourse(ApplicationUser teacher)
    {
        var course = new Course
        {
            TeacherId = teacher.Id,
            Title = "Physics",
            Price = 100m,
            IsPublished = true
        };
        _context.Courses.Add(course);
        _context.SaveChanges();
        return course;
    }

    private PaymentRequest SeedPayment(ApplicationUser student, Course course, string transactionNumber)
    {
        var payment = new PaymentRequest
        {
            StudentId = student.Id,
            CourseId = course.Id,
            Amount = course.Price,
            PaymentMethod = PaymentMethod.VodafoneCash,
            ReceiptImagePath = "/receipts/r.png",
            TransactionNumber = transactionNumber,
            Status = PaymentStatus.Pending
        };
        _context.PaymentRequests.Add(payment);
        _context.SaveChanges();
        return payment;
    }

    [Fact]
    public async Task ApprovePayment_CreatesEnrollment()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher);
        var payment = SeedPayment(student, course, "TXN-1");

        var (success, _) = await _repository.ApprovePaymentAsync(payment.Id, adminId: 1);

        Assert.True(success);
        Assert.Equal(PaymentStatus.Approved, await _context.PaymentRequests.Select(p => p.Status).SingleAsync());
        Assert.Single(await _context.Enrollments.ToListAsync());
    }

    [Fact]
    public async Task ApprovePayment_TwiceOnSameRequest_SecondIsRejected()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher);
        var payment = SeedPayment(student, course, "TXN-1");

        var first = await _repository.ApprovePaymentAsync(payment.Id, adminId: 1);
        var second = await _repository.ApprovePaymentAsync(payment.Id, adminId: 2);

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Single(await _context.Enrollments.ToListAsync());
    }

    [Fact]
    public async Task ApprovePayment_TwoRequestsForSameCourse_SecondStaysPending()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher);
        var first = SeedPayment(student, course, "TXN-1");
        var second = SeedPayment(student, course, "TXN-2");

        var firstResult = await _repository.ApprovePaymentAsync(first.Id, adminId: 1);
        var secondResult = await _repository.ApprovePaymentAsync(second.Id, adminId: 1);

        Assert.True(firstResult.Success);
        // One enrollment per student and course: the second approval is refused
        // (no revenue double-count) and its request stays pending for rejection/refund.
        Assert.False(secondResult.Success);
        Assert.Equal("الطالب مشترك بالفعل في هذا الكورس.", secondResult.Message);
        Assert.Single(await _context.Enrollments.ToListAsync());
        Assert.Equal(PaymentStatus.Pending,
            await _context.PaymentRequests.Where(p => p.Id == second.Id).Select(p => p.Status).SingleAsync());
    }

    [Fact]
    public async Task RejectPayment_TooLongReason_KeepsRequestPending()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher);
        var payment = SeedPayment(student, course, "TXN-1");

        var longReason = new string('x', 301);
        var (success, _) = await _repository.RejectPaymentAsync(payment.Id, adminId: 1, longReason);

        Assert.False(success);
        Assert.Equal(PaymentStatus.Pending, await _context.PaymentRequests.Select(p => p.Status).SingleAsync());
    }

    [Fact]
    public async Task RejectThenApprove_CompeteOnPendingCondition()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher);
        var payment = SeedPayment(student, course, "TXN-1");

        var rejected = await _repository.RejectPaymentAsync(payment.Id, adminId: 1, "bad receipt");
        var approveAfterReject = await _repository.ApprovePaymentAsync(payment.Id, adminId: 2);

        Assert.True(rejected.Success);
        Assert.False(approveAfterReject.Success);
        Assert.Equal(PaymentStatus.Rejected, await _context.PaymentRequests.Select(p => p.Status).SingleAsync());
        Assert.Empty(await _context.Enrollments.ToListAsync());
    }

    [Fact]
    public async Task DuplicateEnrollment_IsRejectedByDatabase()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher);
        var payment = SeedPayment(student, course, "TXN-1");
        await _repository.ApprovePaymentAsync(payment.Id, adminId: 1);
        var secondPayment = SeedPayment(student, course, "TXN-2");

        // The unique index on (StudentId, CourseId) is the database arbitrator
        // that turns a lost enrollment race into a violation the code translates.
        _context.Enrollments.Add(new Enrollment
        {
            StudentId = student.Id,
            CourseId = course.Id,
            PaymentRequestId = secondPayment.Id,
            EnrolledAt = DateTime.UtcNow
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task DuplicatePhoneNumber_IsRejectedByDatabase()
    {
        SeedUser("first@test.com", "01000000001");

        var duplicate = new ApplicationUser
        {
            UserName = "second@test.com",
            Email = "second@test.com",
            FullName = "Second",
            PhoneNumber = "01000000001",
            IsActive = true
        };
        _context.Users.Add(duplicate);

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task NullPhoneNumbers_DoNotConflict()
    {
        SeedUser("first@test.com", null);
        SeedUser("second@test.com", null);

        Assert.Equal(2, await _context.Users.CountAsync());
    }
}
