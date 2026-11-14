namespace EducationalPlatform.Web.Models;

/// <summary>Created when the admin approves a payment request. Gives the student access to the course.</summary>
public class Enrollment
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int PaymentRequestId { get; set; }
    public PaymentRequest PaymentRequest { get; set; } = null!;

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}
