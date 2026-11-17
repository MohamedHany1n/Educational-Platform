
namespace EducationalPlatform.Web.ViewModels;

public class SubmitExamViewModel
{
    public List<SubmitAnswerViewModel> Answers { get; set; }
        = new();
}

public class SubmitAnswerViewModel
{
    public int QuestionId { get; set; }

    public List<int> SelectedOptionIds { get; set; }
        = new();

    public string? EssayText { get; set; }
}
