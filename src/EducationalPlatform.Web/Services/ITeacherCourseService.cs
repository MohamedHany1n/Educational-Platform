using EducationalPlatform.Web.Models.ViewModels.Teacher;

namespace EducationalPlatform.Web.Services;

public interface ITeacherCourseService
{
    Task<List<CourseListItemViewModel>> GetMyCoursesAsync(int teacherId);
    Task<CourseFormViewModel?> GetForEditAsync(int id, int teacherId);
    Task<(bool Success, string Message)> CreateAsync(CourseFormViewModel model, int teacherId);
    Task<(bool Success, string Message)> UpdateAsync(CourseFormViewModel model, int teacherId);
    Task<(bool Success, string Message)> DeleteAsync(int id, int teacherId);
    Task<(bool Success, string Message)> TogglePublishAsync(int id, int teacherId);
}
