namespace EducationalPlatform.Web.Models.ViewModels.Exam;

public class AttemptDetailsViewModel
{
    public int AttemptId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public List<AttemptAnswerDetailsViewModel> Answers { get; set; } = new();
}

public class AttemptAnswerDetailsViewModel
{
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public decimal Marks { get; set; }
    public decimal? Score { get; set; }
    public string? EssayText { get; set; }
    public string? TeacherFeedback { get; set; }

    public List<AttemptOptionDetailsViewModel> Options { get; set; } = new();
}

public class AttemptOptionDetailsViewModel
{
    public string Text { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}