namespace EducationalPlatform.Api.Models;

public class Lesson
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    public ICollection<LessonProgress> Progress { get; set; } = new List<LessonProgress>();
}
