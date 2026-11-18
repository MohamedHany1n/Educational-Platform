namespace EducationalPlatform.Web.ViewModels;

public class TakeExamViewModel
{
    public int AttemptId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public int DurationMinutes { get; set; }

    public List<TakeQuestionViewModel> Questions { get; set; }
        = new();
}

public class TakeQuestionViewModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Marks { get; set; }

    public List<TakeOptionViewModel> Options { get; set; }
        = new();
}

public class TakeOptionViewModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
}
