using EducationalPlatform.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Api.Data;

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---------- Users ----------
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
        });

        // ---------- Parent <-> Student ----------
        builder.Entity<ParentStudent>(e =>
        {
            e.HasKey(x => new { x.ParentId, x.StudentId });
            e.HasOne(x => x.Parent).WithMany(u => u.Children).HasForeignKey(x => x.ParentId);
            e.HasOne(x => x.Student).WithMany(u => u.Parents).HasForeignKey(x => x.StudentId);
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

        // ---------- Exams ----------
        builder.Entity<Exam>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(150).IsRequired();
            e.HasOne(x => x.Course).WithMany(c => c.Exams).HasForeignKey(x => x.CourseId);
        });

        builder.Entity<Question>(e =>
        {
            e.Property(q => q.Type).HasConversion<string>().HasMaxLength(10);
            e.Property(q => q.Marks).HasColumnType("decimal(5,2)");
            e.HasOne(q => q.Exam).WithMany(x => x.Questions)
                .HasForeignKey(q => q.ExamId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuestionOption>(e =>
        {
            e.Property(o => o.Text).HasMaxLength(300).IsRequired();
            e.HasOne(o => o.Question).WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ExamAttempt>(e =>
        {
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.TotalScore).HasColumnType("decimal(6,2)");
            e.HasOne(a => a.Exam).WithMany(x => x.Attempts).HasForeignKey(a => a.ExamId);
            e.HasOne(a => a.Student).WithMany(u => u.ExamAttempts).HasForeignKey(a => a.StudentId);
        });

        builder.Entity<AttemptAnswer>(e =>
        {
            e.Property(a => a.Score).HasColumnType("decimal(5,2)");
            e.Property(a => a.TeacherFeedback).HasMaxLength(500);
            e.HasOne(a => a.Attempt).WithMany(x => x.Answers)
                .HasForeignKey(a => a.AttemptId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Question).WithMany().HasForeignKey(a => a.QuestionId);
            e.HasOne(a => a.SelectedOption).WithMany().HasForeignKey(a => a.SelectedOptionId);
        });

        // SQL Server does not allow several cascade-delete paths to the same table,
        // so nothing is deleted automatically except the parent -> child pairs below.
        // This runs at the END so it also covers relationships configured above.
        var cascadeAllowed = new HashSet<(Type Child, Type Parent)>
        {
            (typeof(Lesson), typeof(Course)),
            (typeof(Question), typeof(Exam)),
            (typeof(QuestionOption), typeof(Question)),
            (typeof(AttemptAnswer), typeof(ExamAttempt))
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