namespace EducationalPlatform.Api.Models;

public class Course
{
    public int Id { get; set; }

    public int TeacherId { get; set; }
    public ApplicationUser Teacher { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ThumbnailPath { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
