namespace EducationalPlatform.Web.Models.ViewModels.Teacher;

public class CourseListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LessonsCount { get; set; }
    public int EnrollmentsCount { get; set; }
}
