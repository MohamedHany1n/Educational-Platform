using EducationalPlatform.Api.Models.Enums;

namespace EducationalPlatform.Api.Models;

/// <summary>A student's request to enroll, with a payment receipt, waiting for admin review.</summary>
public class PaymentRequest
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    /// <summary>Course price at the time of the request (history is not changed if the price changes later).</summary>
    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public string ReceiptImagePath { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public int? ReviewedByAdminId { get; set; }
    public ApplicationUser? ReviewedByAdmin { get; set; }

    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    public Enrollment? Enrollment { get; set; }
}
