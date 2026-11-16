using EducationalPlatform.Web.Models;

namespace EducationalPlatform.Web.Repositories;

public interface ITeacherCourseRepository
{
    Task<List<Course>> GetCoursesByTeacherAsync(int teacherId);
    Task<Course?> GetCourseAsync(int id, int teacherId);
    Task AddCourseAsync(Course course);
    Task SaveChangesAsync();
    Task<bool> HasDependenciesAsync(int courseId);
    Task DeleteCourseAsync(Course course);
}
