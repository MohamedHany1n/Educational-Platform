namespace EducationalPlatform.Web.ViewModels;

public class AvailableExamViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int MaxAttempts { get; set; }
    public int AttemptsUsed { get; set; }
}