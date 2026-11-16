using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationalPlatform.Web.Controllers;

[Authorize(Roles = "Student")]
public class StudentExamsController : Controller
{
    private readonly ApplicationDbContext _context;

    public StudentExamsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Available()
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int studentId))
        {
            return Forbid();
        }

        var exams = await _context.Exams
            .Where(e =>
                e.IsPublished &&
                e.Course.Enrollments.Any(en =>
                    en.StudentId == studentId))
            .Select(e => new AvailableExamViewModel
            {
                Id = e.Id,
                Title = e.Title,
                CourseTitle = e.Course.Title,
                DurationMinutes = e.DurationMinutes,
                MaxAttempts = e.MaxAttempts,
                AttemptsUsed = e.Attempts.Count(a =>
                    a.StudentId == studentId)
            })
            .ToListAsync();

        return View(exams);
    }
}