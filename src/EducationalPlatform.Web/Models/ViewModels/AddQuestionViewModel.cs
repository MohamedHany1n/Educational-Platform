using System.ComponentModel.DataAnnotations;
using EducationalPlatform.Web.Models.Enums;

namespace EducationalPlatform.Web.ViewModels;

public class AddQuestionViewModel
{
    [Required]
    public int ExamId { get; set; }

    [Required(ErrorMessage = "نص السؤال مطلوب")]
    public string Text { get; set; } = string.Empty;

    [Required]
    public QuestionType Type { get; set; }

    [Range(0.01, 999, ErrorMessage = "درجة السؤال يجب أن تكون أكبر من صفر")]
    public decimal Marks { get; set; }

    public List<string> Options { get; set; } = new()
    {
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty
    };

    public List<int> CorrectOptionIndexes { get; set; } = new();
}
