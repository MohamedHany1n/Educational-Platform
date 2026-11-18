namespace EducationalPlatform.Web.Models;

public class AttemptAnswer
{
    public int Id { get; set; }

    public int AttemptId { get; set; }
    public ExamAttempt Attempt { get; set; } = null!;

    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    /// <summary>Chosen option (MCQ questions only).</summary>

    // MCQ answers are stored in SelectedOptions.

    /// <summary>Written answer (Essay questions only).</summary>
    public string? EssayText { get; set; }

    /// <summary>Null until the answer is graded.</summary>
    public decimal? Score { get; set; }
    public string? TeacherFeedback { get; set; }

    public ICollection<AttemptAnswerOption> SelectedOptions { get; set; }
    = new List<AttemptAnswerOption>();
}
