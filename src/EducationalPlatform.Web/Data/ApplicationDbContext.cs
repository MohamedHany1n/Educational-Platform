using EducationalPlatform.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Web.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<PaymentRequest> PaymentRequests => Set<PaymentRequest>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<ParentStudent> ParentStudents => Set<ParentStudent>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();

    public DbSet<AttemptAnswerOption> AttemptAnswerOptions => Set<AttemptAnswerOption>();
    public DbSet<Notification> Notifications => Set<Notification>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---------- Users ----------
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.ParentPhoneNumber).HasMaxLength(15);
            // A phone number belongs to one account only. A unique index cannot sit on
            // nvarchar(max), so the column is bounded at 450 (the standard Identity key
            // length, far beyond any phone format). The index is filtered because
            // SQL Server allows just one NULL in a plain unique index.
            e.Property(u => u.PhoneNumber).HasMaxLength(450);
            e.HasIndex(u => u.PhoneNumber).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL");
        });

        // ---------- Parent <-> Student ----------
        builder.Entity<ParentStudent>(e =>
        {
            e.HasKey(x => new { x.ParentId, x.StudentId });
            e.HasOne(x => x.Parent).WithMany(u => u.Children).HasForeignKey(x => x.ParentId);
            e.HasOne(x => x.Student).WithMany(u => u.Parents).HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.LinkedByAdmin).WithMany().HasForeignKey(x => x.LinkedByAdminId);
        });

        // ---------- Courses & Lessons ----------
        builder.Entity<Course>(e =>
        {
            e.Property(c => c.Title).HasMaxLength(150).IsRequired();
            e.Property(c => c.Price).HasColumnType("decimal(10,2)");
            e.Property(c => c.ThumbnailPath).HasMaxLength(300);
            e.HasOne(c => c.Teacher).WithMany(u => u.Courses).HasForeignKey(c => c.TeacherId);
        });

        builder.Entity<Lesson>(e =>
        {
            e.Property(l => l.Title).HasMaxLength(150).IsRequired();
            e.Property(l => l.VideoUrl).HasMaxLength(400).IsRequired();
            e.HasOne(l => l.Course).WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LessonProgress>(e =>
        {
            e.HasKey(x => new { x.StudentId, x.LessonId });
            e.HasOne(x => x.Student).WithMany(u => u.LessonProgress).HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.Lesson).WithMany(l => l.Progress).HasForeignKey(x => x.LessonId);
        });

        // ---------- Payments & Enrollment ----------
        builder.Entity<PaymentRequest>(e =>
        {
            e.Property(p => p.Amount).HasColumnType("decimal(10,2)");
            e.Property(p => p.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.ReceiptImagePath).HasMaxLength(300).IsRequired();
            e.Property(p => p.RejectionReason).HasMaxLength(300);
            e.Property(p => p.TransactionNumber).HasMaxLength(50).IsRequired();
            e.Property(p => p.SenderPhoneNumber).HasMaxLength(15);
            // The same transaction number cannot be submitted twice for the same payment method.
            e.HasIndex(p => new { p.PaymentMethod, p.TransactionNumber }).IsUnique();
            e.HasOne(p => p.Student).WithMany(u => u.PaymentRequests).HasForeignKey(p => p.StudentId);
            e.HasOne(p => p.Course).WithMany(c => c.PaymentRequests).HasForeignKey(p => p.CourseId);
            e.HasOne(p => p.ReviewedByAdmin).WithMany().HasForeignKey(p => p.ReviewedByAdminId);
        });

        builder.Entity<Enrollment>(e =>
        {
            e.HasOne(x => x.Student).WithMany(u => u.Enrollments).HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.Course).WithMany(c => c.Enrollments).HasForeignKey(x => x.CourseId);
            e.HasOne(x => x.PaymentRequest).WithOne(p => p.Enrollment)
                .HasForeignKey<Enrollment>(x => x.PaymentRequestId);

            // A student cannot enroll twice in the same course.
            e.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
        });

        // ---------- Notifications ----------
        builder.Entity<Notification>(e =>
        {
            e.Property(n => n.Title).HasMaxLength(150).IsRequired();
            e.Property(n => n.Body).HasMaxLength(1000).IsRequired();
            e.Property(n => n.Url).HasMaxLength(300);
            e.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId);
            e.HasIndex(n => new { n.UserId, n.IsRead });
        });

        // ---------- Exams ----------
        builder.Entity<Exam>(e =>
        {
            e.Property(x => x.Title)
                .HasMaxLength(150)
                .IsRequired();

            e.HasOne(x => x.Course)
                .WithMany(c => c.Exams)
                .HasForeignKey(x => x.CourseId);

            e.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Exams_DurationMinutes",
                    "[DurationMinutes] > 0");

                t.HasCheckConstraint(
                    "CK_Exams_MaxAttempts",
                    "[MaxAttempts] > 0");
            });
        });

        builder.Entity<Question>(e =>
        {
            e.Property(q => q.Type)
                .HasConversion<string>()
                .HasMaxLength(10);

            e.Property(q => q.Marks)
                .HasColumnType("decimal(5,2)");

            e.HasOne(q => q.Exam)
                .WithMany(x => x.Questions)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            e.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Questions_Marks",
                    "[Marks] > 0");

                t.HasCheckConstraint(
                    "CK_Questions_Type",
                    "[Type] IN ('MCQ', 'Essay')");
            });
        });

        builder.Entity<QuestionOption>(e =>
        {
            e.Property(o => o.Text).HasMaxLength(300).IsRequired();
            e.HasOne(o => o.Question).WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ExamAttempt>(e =>
        {
            e.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            e.Property(a => a.TotalScore)
                .HasColumnType("decimal(6,2)");

            e.HasOne(a => a.Exam)
                .WithMany(x => x.Attempts)
                .HasForeignKey(a => a.ExamId);

            e.HasOne(a => a.Student)
                .WithMany(u => u.ExamAttempts)
                .HasForeignKey(a => a.StudentId);

            e.HasIndex(a => new
            {
                a.ExamId,
                a.StudentId,
                a.AttemptNumber
            }).IsUnique();

            e.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_ExamAttempts_AttemptNumber",
                    "[AttemptNumber] >= 1");
            });
        });

        builder.Entity<AttemptAnswer>(e =>
        {
            e.Property(a => a.Score).HasColumnType("decimal(5,2)");
            e.Property(a => a.TeacherFeedback).HasMaxLength(500);
            e.HasOne(a => a.Attempt).WithMany(x => x.Answers)
                .HasForeignKey(a => a.AttemptId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Question).WithMany().HasForeignKey(a => a.QuestionId);
            
            e.HasIndex(a => new
            {
                a.AttemptId,
                a.QuestionId
            }).IsUnique();
        });

        builder.Entity<AttemptAnswerOption>(e =>
        {
            e.HasOne(x => x.AttemptAnswer)
                .WithMany(a => a.SelectedOptions)
                .HasForeignKey(x => x.AttemptAnswerId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.QuestionOption)
                .WithMany()
                .HasForeignKey(x => x.QuestionOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new
            {
                x.AttemptAnswerId,
                x.QuestionOptionId
            }).IsUnique();
        });

        // SQL Server does not allow several cascade-delete paths to the same table,
        // so nothing is deleted automatically except the parent -> child pairs below.
        // This runs at the END so it also covers relationships configured above.
        var cascadeAllowed = new HashSet<(Type Child, Type Parent)>
        {
            (typeof(Lesson), typeof(Course)),
            (typeof(Question), typeof(Exam)),
            (typeof(QuestionOption), typeof(Question)),
            (typeof(AttemptAnswer), typeof(ExamAttempt)),
            (typeof(AttemptAnswerOption), typeof(AttemptAnswer))
        };

        foreach (var fk in builder.Model.GetEntityTypes()
                     .Where(e => e.ClrType.Namespace == typeof(Course).Namespace)
                     .SelectMany(e => e.GetForeignKeys()))
        {
            if (!cascadeAllowed.Contains((fk.DeclaringEntityType.ClrType, fk.PrincipalEntityType.ClrType)))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}