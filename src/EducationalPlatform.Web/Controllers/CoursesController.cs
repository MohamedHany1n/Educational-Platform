using EducationalPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EducationalPlatform.Web.Controllers;

/// <summary>FR-10/FR-17/FR-18: student course catalog, enrollment status, and lesson watching.</summary>
[Authorize(Roles = "Student")]
public class CoursesController : Controller
{
    private readonly IStudentService _studentService;

    public CoursesController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    private int StudentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        var courses = await _studentService.GetPublishedCoursesAsync(search, StudentId);
        return View(courses);
    }

    [HttpGet]
    public async Task<IActionResult> My()
    {
        var courses = await _studentService.GetMyCoursesAsync(StudentId);
        return View(courses);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var course = await _studentService.GetCourseDetailsAsync(id, StudentId);
        if (course == null)
            return NotFound();
        return View(course);
    }

    [HttpGet]
    public async Task<IActionResult> Watch(int id)
    {
        var course = await _studentService.GetWatchCourseAsync(id, StudentId);
        if (course == null)
            return Forbid();
        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkLessonCompleted(int lessonId, int courseId)
    {
        var (success, message) = await _studentService.MarkLessonCompletedAsync(lessonId, StudentId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Watch), new { id = courseId });
    }
}
