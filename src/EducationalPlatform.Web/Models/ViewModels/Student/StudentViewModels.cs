using EducationalPlatform.Web.Models.Enums;

namespace EducationalPlatform.Web.Models.ViewModels.Student;

public class CourseCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int LessonsCount { get; set; }
    public bool IsEnrolled { get; set; }
    public bool HasPendingRequest { get; set; }
}

public class CourseDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int LessonsCount { get; set; }
    public bool IsEnrolled { get; set; }
    public bool HasPendingRequest { get; set; }
}

public class MyPaymentRequestViewModel
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

public class WatchLessonViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsCompleted { get; set; }
}

public class WatchCourseViewModel
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public List<WatchLessonViewModel> Lessons { get; set; } = new();
}
