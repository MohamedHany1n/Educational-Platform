namespace EducationalPlatform.Web.Models.ViewModels.Exam;

public class GradeAttemptViewModel
{
    public int AttemptId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }

    public List<GradeAnswerViewModel> Answers { get; set; } = new();
}

public class GradeAnswerViewModel
{
    public int AnswerId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public decimal Marks { get; set; }
    public string? EssayText { get; set; }
    public decimal? Score { get; set; }
    public string? TeacherFeedback { get; set; }
}