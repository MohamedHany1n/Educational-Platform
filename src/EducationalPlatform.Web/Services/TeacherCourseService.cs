using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.ViewModels.Teacher;
using EducationalPlatform.Web.Repositories;

namespace EducationalPlatform.Web.Services;

public class TeacherCourseService : ITeacherCourseService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxThumbnailBytes = 2 * 1024 * 1024;

    private readonly ITeacherCourseRepository _repo;
    private readonly IWebHostEnvironment _env;

    public TeacherCourseService(ITeacherCourseRepository repo, IWebHostEnvironment env)
    {
        _repo = repo;
        _env = env;
    }

    public async Task<List<CourseListItemViewModel>> GetMyCoursesAsync(int teacherId)
    {
        var courses = await _repo.GetCoursesByTeacherAsync(teacherId);
        return courses.Select(c => new CourseListItemViewModel
        {
            Id = c.Id,
            Title = c.Title,
            Price = c.Price,
            IsPublished = c.IsPublished,
            CreatedAt = c.CreatedAt,
            LessonsCount = c.Lessons.Count,
            EnrollmentsCount = c.Enrollments.Count
        }).ToList();
    }

    public async Task<CourseFormViewModel?> GetForEditAsync(int id, int teacherId)
    {
        var course = await _repo.GetCourseAsync(id, teacherId);
        if (course == null) return null;

        return new CourseFormViewModel
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            Price = course.Price,
            IsPublished = course.IsPublished,
            ThumbnailPath = course.ThumbnailPath
        };
    }

    public async Task<(bool Success, string Message)> CreateAsync(CourseFormViewModel model, int teacherId)
    {
        var course = new Course
        {
            TeacherId = teacherId,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            Price = model.Price,
            IsPublished = model.IsPublished
        };

        if (model.Thumbnail != null)
        {
            var (ok, filePath, error) = await SaveThumbnailAsync(model.Thumbnail);
            if (!ok) return (false, error!);
            course.ThumbnailPath = filePath;
        }

        await _repo.AddCourseAsync(course);
        return (true, "تم إنشاء الكورس بنجاح.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(CourseFormViewModel model, int teacherId)
    {
        var course = await _repo.GetCourseAsync(model.Id, teacherId);
        if (course == null) return (false, "الكورس غير موجود.");

        if (model.Thumbnail != null)
        {
            var (ok, filePath, error) = await SaveThumbnailAsync(model.Thumbnail);
            if (!ok) return (false, error!);
            course.ThumbnailPath = filePath;
        }

        course.Title = model.Title.Trim();
        course.Description = model.Description?.Trim();
        course.Price = model.Price;
        course.IsPublished = model.IsPublished;

        await _repo.SaveChangesAsync();
        return (true, "تم تعديل الكورس بنجاح.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, int teacherId)
    {
        var course = await _repo.GetCourseAsync(id, teacherId);
        if (course == null) return (false, "الكورس غير موجود.");

        if (await _repo.HasDependenciesAsync(id))
            return (false, "لا يمكن حذف كورس عليه اشتراكات أو إيصالات أو امتحانات. ألغِ نشره بدلاً من الحذف.");

        await _repo.DeleteCourseAsync(course);
        return (true, "تم حذف الكورس.");
    }

    private async Task<(bool Ok, string? FilePath, string? Error)> SaveThumbnailAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) || file.Length > MaxThumbnailBytes)
            return (false, null, "الصورة لازم تكون JPG أو PNG أو WEBP وبحد أقصى 2 ميجا.");

        var folder = Path.Combine(_env.WebRootPath, "uploads", "thumbnails");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return (true, $"/uploads/thumbnails/{fileName}", null);
    }
}
