using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Web.Repositories;

public class TeacherLessonRepository : ITeacherLessonRepository
{
    private readonly ApplicationDbContext _context;

    public TeacherLessonRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Ownership check: the course must belong to this teacher.
    public async Task<Course?> GetCourseAsync(int courseId, int teacherId)
        => await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);

    public async Task<List<Lesson>> GetLessonsByCourseAsync(int courseId)
        => await _context.Lessons
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.OrderIndex)
            .ThenBy(l => l.Id)
            .ToListAsync();

    // Ownership check through the lesson's course.
    public async Task<Lesson?> GetLessonAsync(int lessonId, int teacherId)
        => await _context.Lessons
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.Course.TeacherId == teacherId);

    public async Task<int> GetNextOrderIndexAsync(int courseId)
    {
        var max = await _context.Lessons
            .Where(l => l.CourseId == courseId)
            .MaxAsync(l => (int?)l.OrderIndex);
        return (max ?? 0) + 1;
    }

    public async Task AddLessonAsync(Lesson lesson)
    {
        await _context.Lessons.AddAsync(lesson);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    public async Task<bool> HasProgressAsync(int lessonId)
        => await _context.LessonProgress.AnyAsync(p => p.LessonId == lessonId);

    public async Task DeleteLessonAsync(Lesson lesson)
    {
        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync();
    }
}
