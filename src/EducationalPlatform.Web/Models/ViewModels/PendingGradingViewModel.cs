namespace EducationalPlatform.Web.ViewModels;

public class PendingGradingViewModel
{
    public int AttemptId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int PendingEssaysCount { get; set; }
}