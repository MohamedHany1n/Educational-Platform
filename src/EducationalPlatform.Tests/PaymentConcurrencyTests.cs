using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EducationalPlatform.Tests;

/// <summary>
/// True concurrent-request races for the admin payment review: two threads run
/// competing operations at the same time against the same database, each on its
/// own connection and DbContext. SQLite serializes writers with a file lock, so
/// the loser observes the winner's committed state, the same ordering guarantee
/// SQL Server's row locks give the conditional UPDATE. The SQL Server-specific
/// error numbers (2601/2627) cannot be produced here; the SQLite constraint
/// error exercises the same IsUniqueViolation path instead.
/// </summary>
public class PaymentConcurrencyTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"payment-race-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        try { if (File.Exists(_databasePath)) File.Delete(_databasePath); }
        catch (IOException) { /* best effort cleanup */ }
    }

    private AdminRepository CreateRepository()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        return new AdminRepository(context);
    }

    private (int studentId, int courseId, int paymentId) Seed()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        var teacher = new ApplicationUser { UserName = "teacher@test.com", Email = "teacher@test.com", FullName = "T", PhoneNumber = "01000000001", IsActive = true };
        var student = new ApplicationUser { UserName = "student@test.com", Email = "student@test.com", FullName = "S", PhoneNumber = "01000000002", IsActive = true };
        context.Users.AddRange(teacher, student);
        context.SaveChanges();

        var course = new Course { TeacherId = teacher.Id, Title = "Physics", Price = 100m, IsPublished = true };
        context.Courses.Add(course);
        context.SaveChanges();

        var payment = new PaymentRequest
        {
            StudentId = student.Id,
            CourseId = course.Id,
            Amount = course.Price,
            PaymentMethod = PaymentMethod.VodafoneCash,
            ReceiptImagePath = "/receipts/r.png",
            TransactionNumber = "TXN-1",
            Status = PaymentStatus.Pending
        };
        context.PaymentRequests.Add(payment);
        context.SaveChanges();
        return (student.Id, course.Id, payment.Id);
    }

    private int AddPayment(int studentId, int courseId, string transactionNumber)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        using var context = new ApplicationDbContext(options);
        var payment = new PaymentRequest
        {
            StudentId = studentId,
            CourseId = courseId,
            Amount = 100m,
            PaymentMethod = PaymentMethod.InstaPay,
            ReceiptImagePath = "/receipts/r2.png",
            TransactionNumber = transactionNumber,
            Status = PaymentStatus.Pending
        };
        context.PaymentRequests.Add(payment);
        context.SaveChanges();
        return payment.Id;
    }

    private (PaymentStatus status, int enrollmentCount) ReadState(int paymentId)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        using var context = new ApplicationDbContext(options);
        var status = context.PaymentRequests.Where(p => p.Id == paymentId).Select(p => p.Status).Single();
        var enrollments = context.Enrollments.Count();
        return (status, enrollments);
    }

    /// <summary>Runs two operations so they overlap in time, then returns both results in start order.</summary>
    private static async Task<((bool Success, string Message) First, (bool Success, string Message) Second)> Race(
        Func<Task<(bool, string)>> first, Func<Task<(bool, string)>> second)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstTask = Task.Run(async () => { await start.Task; return await first(); });
        var secondTask = Task.Run(async () => { await start.Task; return await second(); });
        start.SetResult();
        return (await firstTask, await secondTask);
    }

    [Fact]
    public async Task ConcurrentApprove_SameRequest_ExactlyOneSucceeds()
    {
        var (_, _, paymentId) = Seed();
        var repo1 = CreateRepository();
        var repo2 = CreateRepository();

        var (first, second) = await Race(
            () => repo1.ApprovePaymentAsync(paymentId, adminId: 1),
            () => repo2.ApprovePaymentAsync(paymentId, adminId: 2));

        var results = new[] { first, second };
        Assert.Single(results.Where(r => r.Success));
        var loser = results.Single(r => !r.Success);
        Assert.Equal("تمت مراجعة هذا الطلب مسبقاً.", loser.Message);

        var (status, enrollmentCount) = ReadState(paymentId);
        Assert.Equal(PaymentStatus.Approved, status);
        Assert.Equal(1, enrollmentCount);
    }

    [Fact]
    public async Task ConcurrentApproveAndReject_SameRequest_ExactlyOneWins()
    {
        var (_, _, paymentId) = Seed();
        var approver = CreateRepository();
        var rejecter = CreateRepository();

        var (approveResult, rejectResult) = await Race(
            () => approver.ApprovePaymentAsync(paymentId, adminId: 1),
            () => rejecter.RejectPaymentAsync(paymentId, adminId: 2, "إيصال غير صحيح"));

        Assert.NotEqual(approveResult.Success, rejectResult.Success);
        var loser = approveResult.Success ? rejectResult : approveResult;
        Assert.Equal("تمت مراجعة هذا الطلب مسبقاً.", loser.Message);

        // The final state must match whichever operation won, with no half-state:
        // approval implies the enrollment exists, rejection implies it does not.
        var (status, enrollmentCount) = ReadState(paymentId);
        if (approveResult.Success)
        {
            Assert.Equal(PaymentStatus.Approved, status);
            Assert.Equal(1, enrollmentCount);
        }
        else
        {
            Assert.Equal(PaymentStatus.Rejected, status);
            Assert.Equal(0, enrollmentCount);
        }
    }

    [Fact]
    public async Task ConcurrentApprove_TwoRequestsSameCourse_ExactlyOneEnrollmentAndOneApproval()
    {
        var (studentId, courseId, firstPaymentId) = Seed();
        var secondPaymentId = AddPayment(studentId, courseId, "TXN-2");
        var repo1 = CreateRepository();
        var repo2 = CreateRepository();

        var (first, second) = await Race(
            () => repo1.ApprovePaymentAsync(firstPaymentId, adminId: 1),
            () => repo2.ApprovePaymentAsync(secondPaymentId, adminId: 2));

        var results = new[] { first, second };
        Assert.Single(results.Where(r => r.Success));
        Assert.Equal("الطالب مشترك بالفعل في هذا الكورس.", results.Single(r => !r.Success).Message);

        // Exactly one enrollment, and the losing request rolled its approval back to Pending.
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        using var context = new ApplicationDbContext(options);
        Assert.Equal(1, await context.Enrollments.CountAsync());
        var statuses = await context.PaymentRequests
            .Select(p => p.Status)
            .ToListAsync();
        Assert.Equal(2, statuses.Count);
        Assert.Single(statuses.Where(s => s == PaymentStatus.Approved));
        Assert.Single(statuses.Where(s => s == PaymentStatus.Pending));
        Assert.Equal(studentId, (await context.Enrollments.SingleAsync()).StudentId);
        Assert.Equal(courseId, (await context.Enrollments.SingleAsync()).CourseId);
    }
}
