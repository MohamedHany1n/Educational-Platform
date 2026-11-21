using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EducationalPlatform.Tests;

/// <summary>
/// In-app notifications on payment review: approving or rejecting a payment
/// request must create a notification for the student inside the same
/// transaction as the review itself.
/// </summary>
public class AdminNotificationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly AdminRepository _repository;

    public AdminNotificationTests()
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

    private PaymentRequest SeedPendingPayment(string transactionNumber)
    {
        var teacher = new ApplicationUser
        {
            UserName = "teacher@test.com", Email = "teacher@test.com",
            FullName = "Teacher", PhoneNumber = "01000000001", IsActive = true
        };
        var student = new ApplicationUser
        {
            UserName = "student@test.com", Email = "student@test.com",
            FullName = "Student", PhoneNumber = "01000000002", IsActive = true
        };
        _context.Users.AddRange(teacher, student);
        _context.SaveChanges();

        var course = new Course { TeacherId = teacher.Id, Title = "Physics", Price = 100m, IsPublished = true };
        _context.Courses.Add(course);
        _context.SaveChanges();

        var payment = new PaymentRequest
        {
            StudentId = student.Id,
            CourseId = course.Id,
            Amount = course.Price,
            PaymentMethod = PaymentMethod.VodafoneCash,
            ReceiptImagePath = "r.png",
            TransactionNumber = transactionNumber,
            Status = PaymentStatus.Pending
        };
        _context.PaymentRequests.Add(payment);
        _context.SaveChanges();
        return payment;
    }

    [Fact]
    public async Task ApprovePayment_CreatesApprovalNotification()
    {
        var payment = SeedPendingPayment("TXN-N1");

        var (success, message) = await _repository.ApprovePaymentAsync(payment.Id, adminId: 1);

        Assert.True(success, message);
        var notification = await _context.Notifications.SingleAsync();
        Assert.Equal(payment.StudentId, notification.UserId);
        Assert.Contains("قبول", notification.Title);
        Assert.Contains("Physics", notification.Body);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task RejectPayment_CreatesRejectionNotificationWithReason()
    {
        var payment = SeedPendingPayment("TXN-N2");

        var (success, message) = await _repository.RejectPaymentAsync(payment.Id, adminId: 1, "الإيصال غير واضح");

        Assert.True(success, message);
        var notification = await _context.Notifications.SingleAsync();
        Assert.Equal(payment.StudentId, notification.UserId);
        Assert.Contains("رفض", notification.Title);
        Assert.Contains("الإيصال غير واضح", notification.Body);
    }

    [Fact]
    public async Task RejectPayment_AlreadyReviewed_NoNotification()
    {
        var payment = SeedPendingPayment("TXN-N3");

        var first = await _repository.RejectPaymentAsync(payment.Id, adminId: 1, "سبب أول");
        var second = await _repository.RejectPaymentAsync(payment.Id, adminId: 1, "سبب ثان");

        Assert.True(first.Success);
        Assert.False(second.Success);
        // Exactly one review happened, so exactly one notification exists.
        Assert.Equal(1, await _context.Notifications.CountAsync());
    }
}
