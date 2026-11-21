using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EducationalPlatform.Tests;

/// <summary>
/// SQL Server versions of the payment concurrency races. These are the
/// provider-authoritative checks: SQLite serializes whole-database writers,
/// while SQL Server exercises the real row-lock blocking, the 2601 unique-index
/// error classification, and rollback-after-violation (XACT_STATE) behavior.
///
/// They run only when the EDU_PLATFORM_TEST_SQLSERVER environment variable
/// holds a SQL Server connection string, for example a local throwaway
/// LocalDB instance:
///
///   set "EDU_PLATFORM_TEST_SQLSERVER=Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;"
///   dotnet test --filter "FullyQualifiedName~SqlServerPaymentConcurrencyTests"
///
/// Each run creates its own scratch database (EducationalPlatformRaceTest_...)
/// and deletes it afterwards; it never touches an existing database. When the
/// environment variable is absent the tests return immediately, so the suite
/// stays green on machines without SQL Server.
/// </summary>
public class SqlServerPaymentConcurrencyTests
{
    private static string? BaseConnectionString
        => Environment.GetEnvironmentVariable("EDU_PLATFORM_TEST_SQLSERVER");

    private sealed class ScratchDatabase : IDisposable
    {
        public string Name { get; } = $"EducationalPlatformRaceTest_{Guid.NewGuid():N}";
        public string ConnectionString { get; }

        public ScratchDatabase(string baseConnectionString)
        {
            ConnectionString = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = Name
            }.ConnectionString;

            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        public ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;
            return new ApplicationDbContext(options);
        }

        public void Dispose()
        {
            try
            {
                using var context = CreateContext();
                context.Database.EnsureDeleted();
            }
            catch (SqlException) { /* best effort cleanup of the scratch database */ }
        }
    }

    private static (int studentId, int courseId, int paymentId) Seed(ApplicationDbContext context, string transactionNumber)
    {
        var teacher = new ApplicationUser { UserName = $"teacher-{Guid.NewGuid():N}@test.com", Email = "teacher@test.com", FullName = "T", PhoneNumber = null, IsActive = true };
        var student = new ApplicationUser { UserName = $"student-{Guid.NewGuid():N}@test.com", Email = "student@test.com", FullName = "S", PhoneNumber = null, IsActive = true };
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
            TransactionNumber = transactionNumber,
            Status = PaymentStatus.Pending
        };
        context.PaymentRequests.Add(payment);
        context.SaveChanges();
        return (student.Id, course.Id, payment.Id);
    }

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
    public async Task SqlServer_ConcurrentApprove_SameRequest_ExactlyOneSucceeds()
    {
        if (BaseConnectionString is not { } cs) return;
        using var db = new ScratchDatabase(cs);
        int paymentId;
        await using (var seedContext = db.CreateContext())
            paymentId = Seed(seedContext, "TXN-1").paymentId;

        await using var context1 = db.CreateContext();
        await using var context2 = db.CreateContext();
        var repo1 = new AdminRepository(context1);
        var repo2 = new AdminRepository(context2);

        var (first, second) = await Race(
            () => repo1.ApprovePaymentAsync(paymentId, adminId: 1),
            () => repo2.ApprovePaymentAsync(paymentId, adminId: 2));

        var results = new[] { first, second };
        Assert.Single(results.Where(r => r.Success));
        Assert.Equal("تمت مراجعة هذا الطلب مسبقاً.", results.Single(r => !r.Success).Message);

        await using var readContext = db.CreateContext();
        Assert.Equal(PaymentStatus.Approved,
            await readContext.PaymentRequests.Where(p => p.Id == paymentId).Select(p => p.Status).SingleAsync());
        Assert.Equal(1, await readContext.Enrollments.CountAsync());
    }

    [Fact]
    public async Task SqlServer_ConcurrentApproveAndReject_SameRequest_ExactlyOneWins()
    {
        if (BaseConnectionString is not { } cs) return;
        using var db = new ScratchDatabase(cs);
        int paymentId;
        await using (var seedContext = db.CreateContext())
            paymentId = Seed(seedContext, "TXN-1").paymentId;

        await using var context1 = db.CreateContext();
        await using var context2 = db.CreateContext();
        var approver = new AdminRepository(context1);
        var rejecter = new AdminRepository(context2);

        var (approveResult, rejectResult) = await Race(
            () => approver.ApprovePaymentAsync(paymentId, adminId: 1),
            () => rejecter.RejectPaymentAsync(paymentId, adminId: 2, "إيصال غير صحيح"));

        Assert.NotEqual(approveResult.Success, rejectResult.Success);
        var loser = approveResult.Success ? rejectResult : approveResult;
        Assert.Equal("تمت مراجعة هذا الطلب مسبقاً.", loser.Message);

        await using var readContext = db.CreateContext();
        var status = await readContext.PaymentRequests.Where(p => p.Id == paymentId).Select(p => p.Status).SingleAsync();
        var enrollments = await readContext.Enrollments.CountAsync();
        if (approveResult.Success)
        {
            Assert.Equal(PaymentStatus.Approved, status);
            Assert.Equal(1, enrollments);
        }
        else
        {
            Assert.Equal(PaymentStatus.Rejected, status);
            Assert.Equal(0, enrollments);
        }
    }

    [Fact]
    public async Task SqlServer_ConcurrentApprove_TwoRequestsSameCourse_ExactlyOneApprovalAndLoserRollsBack()
    {
        if (BaseConnectionString is not { } cs) return;
        using var db = new ScratchDatabase(cs);
        int firstPaymentId, secondPaymentId;
        await using (var seedContext = db.CreateContext())
        {
            var (studentId, courseId, paymentId) = Seed(seedContext, "TXN-1");
            firstPaymentId = paymentId;
            var secondPayment = new PaymentRequest
            {
                StudentId = studentId,
                CourseId = courseId,
                Amount = 100m,
                PaymentMethod = PaymentMethod.InstaPay,
                ReceiptImagePath = "/receipts/r2.png",
                TransactionNumber = "TXN-2",
                Status = PaymentStatus.Pending
            };
            seedContext.PaymentRequests.Add(secondPayment);
            seedContext.SaveChanges();
            secondPaymentId = secondPayment.Id;
        }

        await using var context1 = db.CreateContext();
        await using var context2 = db.CreateContext();
        var repo1 = new AdminRepository(context1);
        var repo2 = new AdminRepository(context2);

        var (first, second) = await Race(
            () => repo1.ApprovePaymentAsync(firstPaymentId, adminId: 1),
            () => repo2.ApprovePaymentAsync(secondPaymentId, adminId: 2));

        var results = new[] { first, second };
        Assert.Single(results.Where(r => r.Success));
        Assert.Equal("الطالب مشترك بالفعل في هذا الكورس.", results.Single(r => !r.Success).Message);

        // The loser must have rolled its approval back to Pending: this is the
        // real-SQL-Server check that a rolled-back transaction after a unique
        // violation leaves no half-committed state.
        await using var readContext = db.CreateContext();
        var statuses = await readContext.PaymentRequests.Select(p => p.Status).ToListAsync();
        Assert.Equal(2, statuses.Count);
        Assert.Single(statuses.Where(s => s == PaymentStatus.Approved));
        Assert.Single(statuses.Where(s => s == PaymentStatus.Pending));
        Assert.Equal(1, await readContext.Enrollments.CountAsync());
    }

    [Fact]
    public async Task SqlServer_DuplicateEnrollment_Throws2601Or2627MatchingIsUniqueViolation()
    {
        if (BaseConnectionString is not { } cs) return;
        using var db = new ScratchDatabase(cs);
        await using var context = db.CreateContext();
        var (studentId, courseId, paymentId) = Seed(context, "TXN-1");

        context.Enrollments.Add(new Enrollment { StudentId = studentId, CourseId = courseId, PaymentRequestId = paymentId, EnrolledAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var secondPayment = new PaymentRequest
        {
            StudentId = studentId,
            CourseId = courseId,
            Amount = 100m,
            PaymentMethod = PaymentMethod.InstaPay,
            ReceiptImagePath = "/receipts/r2.png",
            TransactionNumber = "TXN-2",
            Status = PaymentStatus.Pending
        };
        context.PaymentRequests.Add(secondPayment);
        await context.SaveChangesAsync();

        // Deterministic check of the exact SQL Server error numbers the
        // repository's IsUniqueViolation filter matches on.
        context.Enrollments.Add(new Enrollment { StudentId = studentId, CourseId = courseId, PaymentRequestId = secondPayment.Id, EnrolledAt = DateTime.UtcNow });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.IsType<SqlException>(ex.InnerException);
        var sqlException = (SqlException)ex.InnerException!;
        Assert.True(sqlException.Number is 2601 or 2627,
            $"Expected SQL Server unique violation 2601/2627, got {sqlException.Number}.");
    }
}
