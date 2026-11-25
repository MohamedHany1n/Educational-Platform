
namespace EducationalPlatform.Web.Models.ViewModels.Exam;

public class LeaderboardStudentViewModel
{
    public int Rank { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal BestScore { get; set; }
}
