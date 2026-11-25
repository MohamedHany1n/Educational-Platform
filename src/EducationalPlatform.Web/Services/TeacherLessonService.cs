using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.ViewModels.Teacher;
using EducationalPlatform.Web.Repositories;

namespace EducationalPlatform.Web.Services;

public class TeacherLessonService : ITeacherLessonService
{
    private readonly ITeacherLessonRepository _repo;

    public TeacherLessonService(ITeacherLessonRepository repo)
    {
        _repo = repo;
    }

    public async Task<CourseLessonsViewModel?> GetCourseLessonsAsync(int courseId, int teacherId)
    {
        var course = await _repo.GetCourseAsync(courseId, teacherId);
        if (course == null) return null;

        var lessons = await _repo.GetLessonsByCourseAsync(courseId);
        return new CourseLessonsViewModel
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            Lessons = lessons.Select(l => new LessonListItemViewModel
            {
                Id = l.Id,
                OrderIndex = l.OrderIndex,
                Title = l.Title,
                VideoUrl = l.VideoUrl
            }).ToList()
        };
    }

    public async Task<LessonFormViewModel?> GetNewFormAsync(int courseId, int teacherId)
    {
        var course = await _repo.GetCourseAsync(courseId, teacherId);
        if (course == null) return null;

        return new LessonFormViewModel
        {
            CourseId = course.Id,
            OrderIndex = await _repo.GetNextOrderIndexAsync(course.Id)
        };
    }

    public async Task<LessonFormViewModel?> GetForEditAsync(int lessonId, int teacherId)
    {
        var lesson = await _repo.GetLessonAsync(lessonId, teacherId);
        if (lesson == null) return null;

        return new LessonFormViewModel
        {
            Id = lesson.Id,
            CourseId = lesson.CourseId,
            Title = lesson.Title,
            Description = lesson.Description,
            VideoUrl = lesson.VideoUrl,
            OrderIndex = lesson.OrderIndex
        };
    }

    public async Task<(bool Success, string Message)> CreateAsync(LessonFormViewModel model, int teacherId)
    {
        var course = await _repo.GetCourseAsync(model.CourseId, teacherId);
        if (course == null) return (false, "الكورس غير موجود.");

        var videoUrl = model.VideoUrl.Trim();
        if (!IsValidVideoUrl(videoUrl))
            return (false, "رابط الفيديو لازم يبدأ بـ http:// أو https://");

        var lesson = new Lesson
        {
            CourseId = course.Id,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            VideoUrl = videoUrl,
            OrderIndex = model.OrderIndex
        };

        await _repo.AddLessonAsync(lesson);
        return (true, "تم إضافة الدرس بنجاح.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(LessonFormViewModel model, int teacherId)
    {
        // The lesson stays in its own course; model.CourseId from the form is ignored.
        var lesson = await _repo.GetLessonAsync(model.Id, teacherId);
        if (lesson == null) return (false, "الدرس غير موجود.");

        var videoUrl = model.VideoUrl.Trim();
        if (!IsValidVideoUrl(videoUrl))
            return (false, "رابط الفيديو لازم يبدأ بـ http:// أو https://");

        lesson.Title = model.Title.Trim();
        lesson.Description = model.Description?.Trim();
        lesson.VideoUrl = videoUrl;
        lesson.OrderIndex = model.OrderIndex;

        await _repo.SaveChangesAsync();
        return (true, "تم تعديل الدرس بنجاح.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int lessonId, int teacherId)
    {
        var lesson = await _repo.GetLessonAsync(lessonId, teacherId);
        if (lesson == null) return (false, "الدرس غير موجود.");

        // LessonProgress -> Lesson is Restrict, so deleting a watched lesson would fail.
        if (await _repo.HasProgressAsync(lessonId))
            return (false, "لا يمكن حذف درس شاهده طلاب. عدّله بدلاً من حذفه.");

        await _repo.DeleteLessonAsync(lesson);
        return (true, "تم حذف الدرس.");
    }

    public async Task<(bool Success, string Message)> MoveAsync(int lessonId, int teacherId, bool up)
    {
        var lesson = await _repo.GetLessonAsync(lessonId, teacherId);
        if (lesson == null) return (false, "الدرس غير موجود.");

        // Tracked entities, ordered by OrderIndex then Id.
        var lessons = await _repo.GetLessonsByCourseAsync(lesson.CourseId);
        var index = lessons.FindIndex(l => l.Id == lessonId);
        var target = up ? index - 1 : index + 1;

        // Already first or last: nothing to do.
        if (target < 0 || target >= lessons.Count) return (true, string.Empty);

        (lessons[index], lessons[target]) = (lessons[target], lessons[index]);

        // Renumber 1..n so duplicate or gapped OrderIndex values are cleaned up too.
        for (var i = 0; i < lessons.Count; i++)
            lessons[i].OrderIndex = i + 1;

        await _repo.SaveChangesAsync();
        return (true, string.Empty);
    }

    private static bool IsValidVideoUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
