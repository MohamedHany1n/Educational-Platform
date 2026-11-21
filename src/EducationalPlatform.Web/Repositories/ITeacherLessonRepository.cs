using EducationalPlatform.Web.Models;

namespace EducationalPlatform.Web.Repositories;

public interface ITeacherLessonRepository
{
    Task<Course?> GetCourseAsync(int courseId, int teacherId);
    Task<List<Lesson>> GetLessonsByCourseAsync(int courseId);
    Task<Lesson?> GetLessonAsync(int lessonId, int teacherId);
    Task<int> GetNextOrderIndexAsync(int courseId);
    Task AddLessonAsync(Lesson lesson);
    Task SaveChangesAsync();
    Task<bool> HasProgressAsync(int lessonId);
    Task DeleteLessonAsync(Lesson lesson);
}
