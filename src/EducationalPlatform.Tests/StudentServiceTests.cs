using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EducationalPlatform.Tests;

/// <summary>
/// Student enrollment/payment service tests (FR-10..18) on SQLite in memory,
/// with a stub receipt storage so validation behavior is controllable.
/// </summary>
public class StudentServiceTests : IDisposable
{
    private class StubReceiptStorage : IReceiptStorage
    {
        public bool Accept { get; set; } = true;

        public Task<string?> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default)
            => Task.FromResult(Accept ? "stored-receipt.png" : (string?)null);

        public Task<(Stream Content, string ContentType)?> OpenAsync(string storedPath, CancellationToken cancellationToken = default)
            => Task.FromResult<(Stream, string)?>(null);
    }

    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly StubReceiptStorage _receiptStorage;
    private readonly StudentService _service;

    public StudentServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _receiptStorage = new StubReceiptStorage();
        _service = new StudentService(_context, _receiptStorage);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private ApplicationUser SeedUser(string email, string phone)
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

    private Course SeedCourse(ApplicationUser teacher, decimal price = 100m, bool published = true)
    {
        var course = new Course
        {
            TeacherId = teacher.Id,
            Title = "Physics",
            Price = price,
            IsPublished = published
        };
        _context.Courses.Add(course);
        _context.SaveChanges();
        return course;
    }

    private static Stream ReceiptStream() => new MemoryStream(new byte[] { 1, 2, 3 });

    /// <summary>Seeds an approved payment + enrollment, mirroring what admin approval creates.</summary>
    private void SeedEnrollment(ApplicationUser student, Course course, string transactionNumber)
    {
        var payment = new PaymentRequest
        {
            StudentId = student.Id,
            CourseId = course.Id,
            Amount = course.Price,
            PaymentMethod = PaymentMethod.VodafoneCash,
            ReceiptImagePath = "seed.png",
            TransactionNumber = transactionNumber,
            Status = PaymentStatus.Approved
        };
        _context.PaymentRequests.Add(payment);
        _context.SaveChanges();
        _context.Enrollments.Add(new Enrollment
        {
            StudentId = student.Id,
            CourseId = course.Id,
            PaymentRequestId = payment.Id
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task SubmitPayment_HappyPath_SnapshotsCoursePrice()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher, price: 250m);

        var (success, message) = await _service.SubmitPaymentRequestAsync(
            student.Id, course.Id, PaymentMethod.VodafoneCash, "TXN-1", "01012345678",
            ReceiptStream(), "receipt.png");

        Assert.True(success, message);
        var request = await _context.PaymentRequests.SingleAsync();
        Assert.Equal(PaymentStatus.Pending, request.Status);
        Assert.Equal(250m, request.Amount);
        Assert.Equal("stored-receipt.png", request.ReceiptImagePath);
        Assert.Equal("01012345678", request.SenderPhoneNumber);

        // FR-12: a later price change must not alter the submitted amount.
        course.Price = 999m;
        await _context.SaveChangesAsync();
        Assert.Equal(250m, await _context.PaymentRequests.Select(p => p.Amount).SingleAsync());
    }

    [Fact]
    public async Task SubmitPayment_ReusesTransactionNumber_Rejected()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher);

        var first = await _service.SubmitPaymentRequestAsync(
            student.Id, course.Id, PaymentMethod.VodafoneCash, "TXN-DUP", null,
            ReceiptStream(), "a.png");
        Assert.True(first.Success);

        // Same transaction number with the same method must be rejected (FR-32),
        // even for a different course.
        var course2 = SeedCourse(teacher);
        var second = await _service.SubmitPaymentRequestAsync(
            student.Id, course2.Id, PaymentMethod.VodafoneCash, "TXN-DUP", null,
            ReceiptStream(), "b.png");

        Assert.False(second.Success);
        Assert.Equal(1, await _context.PaymentRequests.CountAsync());
    }

    [Fact]
    public async Task SubmitPayment_SecondPendingForSameCourse_Rejected()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher);

        var first = await _service.SubmitPaymentRequestAsync(
            student.Id, course.Id, PaymentMethod.VodafoneCash, "TXN-A", null,
            ReceiptStream(), "a.png");
        Assert.True(first.Success);

        var second = await _service.SubmitPaymentRequestAsync(
            student.Id, course.Id, PaymentMethod.InstaPay, "TXN-B", null,
            ReceiptStream(), "b.png");

        Assert.False(second.Success);
        Assert.Equal(1, await _context.PaymentRequests.CountAsync());
    }

    [Fact]
    public async Task SubmitPayment_AlreadyEnrolled_Rejected()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher);
        SeedEnrollment(student, course, "TXN-SEED-1");

        var (success, _) = await _service.SubmitPaymentRequestAsync(
            student.Id, course.Id, PaymentMethod.VodafoneCash, "TXN-E", null,
            ReceiptStream(), "a.png");

        Assert.False(success);
        // Only the seeded enrollment's payment request exists.
        Assert.Equal(1, await _context.PaymentRequests.CountAsync());
    }

    [Fact]
    public async Task SubmitPayment_ReceiptRejectedByStorage_NoRequestCreated()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher);
        _receiptStorage.Accept = false;

        var (success, message) = await _service.SubmitPaymentRequestAsync(
            student.Id, course.Id, PaymentMethod.VodafoneCash, "TXN-R", null,
            ReceiptStream(), "evil.exe");

        Assert.False(success);
        Assert.Contains("الإيصال", message);
        Assert.Empty(_context.PaymentRequests);
    }

    [Fact]
    public async Task MarkLessonCompleted_TogglesProgress()
    {
        var teacher = SeedUser("teacher@test.com", "01000000001");
        var student = SeedUser("student@test.com", "01000000002");
        var course = SeedCourse(teacher);
        var lesson = new Lesson { CourseId = course.Id, Title = "L1", VideoUrl = "https://v/1", OrderIndex = 1 };
        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();
        SeedEnrollment(student, course, "TXN-SEED-2");

        var (success, _) = await _service.MarkLessonCompletedAsync(lesson.Id, student.Id);

        Assert.True(success);
        var progress = await _context.LessonProgress.SingleAsync();
        Assert.True(progress.IsCompleted);
        Assert.NotNull(progress.CompletedAt);
    }
}
