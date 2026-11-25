using EducationalPlatform.Web.Models.ViewModels.Teacher;

namespace EducationalPlatform.Web.Services;

public interface ITeacherLessonService
{
    Task<CourseLessonsViewModel?> GetCourseLessonsAsync(int courseId, int teacherId);
    Task<LessonFormViewModel?> GetNewFormAsync(int courseId, int teacherId);
    Task<LessonFormViewModel?> GetForEditAsync(int lessonId, int teacherId);
    Task<(bool Success, string Message)> CreateAsync(LessonFormViewModel model, int teacherId);
    Task<(bool Success, string Message)> UpdateAsync(LessonFormViewModel model, int teacherId);
    Task<(bool Success, string Message)> DeleteAsync(int lessonId, int teacherId);
    Task<(bool Success, string Message)> MoveAsync(int lessonId, int teacherId, bool up);
}
