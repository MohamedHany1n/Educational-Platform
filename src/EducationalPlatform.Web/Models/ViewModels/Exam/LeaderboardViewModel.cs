
namespace EducationalPlatform.Web.Models.ViewModels.Exam;

public class LeaderboardViewModel
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int PageSize { get; set; } = 10;

    public List<LeaderboardStudentViewModel> Students { get; set; } = new();
}
