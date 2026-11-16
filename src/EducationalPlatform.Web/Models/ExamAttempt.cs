using EducationalPlatform.Web.Models.Enums;

namespace EducationalPlatform.Web.Models;

/// <summary>One try of a student at an exam. TotalScore is the student's grade.</summary>
public class ExamAttempt
{
    public int Id { get; set; }

    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;

    public int StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;

    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    public decimal? TotalScore { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    public int AttemptNumber { get; set; }
    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}
