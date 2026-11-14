namespace EducationalPlatform.Web.Models;

/// <summary>Which lessons a student has completed.</summary>
public class LessonProgress
{
    public int StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;

    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}
