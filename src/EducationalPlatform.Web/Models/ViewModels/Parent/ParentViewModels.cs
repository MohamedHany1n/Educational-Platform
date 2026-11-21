namespace EducationalPlatform.Web.Models.ViewModels.Parent;

public class ParentChildViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentPhone { get; set; } = string.Empty;
    public DateTime LinkedAt { get; set; }
    public int EnrolledCourseCount { get; set; }
    public int PendingPaymentCount { get; set; }
}

public class ParentChildDetailsViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentPhone { get; set; } = string.Empty;
    public List<ParentChildCourseProgressViewModel> Courses { get; set; } = new();
}

public class ParentChildCourseProgressViewModel
{
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public List<ParentChildExamGradeViewModel> Grades { get; set; } = new();
    public int PendingGradingCount { get; set; }
}

public class ParentChildExamGradeViewModel
{
    public string ExamTitle { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public decimal TotalMarks { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
