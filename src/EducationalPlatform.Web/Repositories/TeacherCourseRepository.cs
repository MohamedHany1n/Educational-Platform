using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Web.Repositories;

public class TeacherCourseRepository : ITeacherCourseRepository
{
    private readonly ApplicationDbContext _context;

    public TeacherCourseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Course>> GetCoursesByTeacherAsync(int teacherId)
        => await _context.Courses
            .Where(c => c.TeacherId == teacherId)
            .Include(c => c.Lessons)
            .Include(c => c.Enrollments)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    // Filtering by teacherId prevents a teacher from opening another teacher's course.
    public async Task<Course?> GetCourseAsync(int id, int teacherId)
        => await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == teacherId);

    public async Task AddCourseAsync(Course course)
    {
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    public async Task<bool> HasDependenciesAsync(int courseId)
        => await _context.Enrollments.AnyAsync(e => e.CourseId == courseId)
        || await _context.PaymentRequests.AnyAsync(p => p.CourseId == courseId)
        || await _context.Exams.AnyAsync(x => x.CourseId == courseId);

    public async Task DeleteCourseAsync(Course course)
    {
        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
    }
}
