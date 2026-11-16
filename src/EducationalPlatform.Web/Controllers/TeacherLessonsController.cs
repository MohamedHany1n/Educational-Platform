using System.Security.Claims;
using EducationalPlatform.Web.Models.ViewModels.Teacher;
using EducationalPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EducationalPlatform.Web.Controllers;

[Authorize(Roles = "Teacher")]
public class TeacherLessonsController : Controller
{
    private readonly ITeacherLessonService _service;

    public TeacherLessonsController(ITeacherLessonService service)
    {
        _service = service;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(int courseId)
    {
        var model = await _service.GetCourseLessonsAsync(courseId, CurrentUserId);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int courseId)
    {
        var model = await _service.GetNewFormAsync(courseId, CurrentUserId);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LessonFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, message) = await _service.CreateAsync(model, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index), new { courseId = model.CourseId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _service.GetForEditAsync(id, CurrentUserId);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LessonFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var (success, message) = await _service.UpdateAsync(model, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index), new { courseId = model.CourseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int courseId)
    {
        var (success, message) = await _service.DeleteAsync(id, CurrentUserId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index), new { courseId });
    }
}
