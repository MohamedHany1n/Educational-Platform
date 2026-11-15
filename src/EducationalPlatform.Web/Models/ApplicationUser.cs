using Microsoft.AspNetCore.Identity;

namespace EducationalPlatform.Web.Models;


public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Soft delete: the account is hidden everywhere but its history (payments, progress, grades) is kept.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Student only: the parent's phone number, entered at registration. The admin uses it to create and link the parent account.</summary>
    public string? ParentPhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Teacher side
    public ICollection<Course> Courses { get; set; } = new List<Course>();

    // Student side
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();
    public ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();

    // Parent <-> Student links
    public ICollection<ParentStudent> Children { get; set; } = new List<ParentStudent>();
    public ICollection<ParentStudent> Parents { get; set; } = new List<ParentStudent>();
}
