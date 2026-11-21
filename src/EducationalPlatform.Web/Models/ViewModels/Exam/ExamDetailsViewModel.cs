namespace EducationalPlatform.Web.Models.ViewModels.Exam;

public class ExamDetailsViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string CourseTitle { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public int QuestionsCount { get; set; }

    public decimal TotalMarks { get; set; }

    public int MaxAttempts { get; set; }

    public int AttemptsUsed { get; set; }

    public int RemainingAttempts => MaxAttempts - AttemptsUsed;
}
