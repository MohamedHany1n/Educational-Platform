using EducationalPlatform.Web.Models.Enums;

namespace EducationalPlatform.Web.Models.ViewModels.Admin;

public class StudentDetailsViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ParentPhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public string StatusKey => IsDeleted ? "deleted"
        : IsActive ? "active"
        : ApprovedAt == null ? "pending"
        : "suspended";

    public List<StudentCourseProgressViewModel> Courses { get; set; } = new();
    public List<StudentExamResultViewModel> Exams { get; set; } = new();
    public List<StudentPaymentViewModel> Payments { get; set; } = new();
    public List<StudentParentViewModel> Parents { get; set; } = new();
}

public class StudentCourseProgressViewModel
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }

    public int Percent => TotalLessons == 0 ? 0 : (int)Math.Round(CompletedLessons * 100.0 / TotalLessons);
}

public class StudentExamResultViewModel
{
    public int AttemptId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public AttemptStatus Status { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal MaxScore { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public class StudentPaymentViewModel
{
    public int Id { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StudentParentViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime LinkedAt { get; set; }
}
