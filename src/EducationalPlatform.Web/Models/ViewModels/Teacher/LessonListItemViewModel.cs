namespace EducationalPlatform.Web.Models.ViewModels.Teacher;

public class LessonListItemViewModel
{
    public int Id { get; set; }
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
}

public class CourseLessonsViewModel
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public List<LessonListItemViewModel> Lessons { get; set; } = new();
}
