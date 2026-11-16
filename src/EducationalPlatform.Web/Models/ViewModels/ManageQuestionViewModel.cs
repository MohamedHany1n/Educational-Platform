using EducationalPlatform.Web.Models.Enums;

namespace EducationalPlatform.Web.ViewModels;

public class ManageQuestionViewModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public decimal Marks { get; set; }
    public int OptionsCount { get; set; }
}