using EducationalPlatform.Web.Models.Enums;

namespace EducationalPlatform.Web.Models;

public class Question
{
    public int Id { get; set; }

    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;

    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public decimal Marks { get; set; }

    /// <summary>Only used for MCQ questions.</summary>
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}
