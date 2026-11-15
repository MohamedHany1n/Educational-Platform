using System.Security.Claims;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Models.ViewModels.Admin;
using EducationalPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EducationalPlatform.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await _adminService.GetDashboardStatsAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Teachers()
    {
        var teachers = await _adminService.GetTeachersAsync();
        return View(teachers);
    }

    [HttpGet]
    public IActionResult CreateTeacher()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTeacher(CreateTeacherViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, errors) = await _adminService.CreateTeacherAsync(model);
        if (success)
        {
            TempData["SuccessMessage"] = $"تم إنشاء حساب المدرس ({model.FullName}) بنجاح.";
            return RedirectToAction(nameof(Teachers));
        }

        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTeacherStatus(int id)
    {
        var (success, message) = await _adminService.ToggleTeacherStatusAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Teachers));
    }

    // ---------------- Parents ----------------

    [HttpGet]
    public async Task<IActionResult> Parents()
    {
        var parents = await _adminService.GetParentsAsync();
        return View(parents);
    }

    [HttpGet]
    public async Task<IActionResult> CreateParent(int? studentId = null)
    {
        var (model, existingParentId) = await _adminService.PrepareCreateParentAsync(studentId);

        // A parent with the same phone already exists (for example a second child): link instead of creating a duplicate.
        if (existingParentId.HasValue)
        {
            TempData["SuccessMessage"] = "يوجد حساب ولي أمر بنفس الرقم. اربط الطالب بهذا الحساب بدلاً من إنشاء حساب جديد.";
            return RedirectToAction(nameof(ParentDetails), new { id = existingParentId.Value, studentId });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateParent(CreateParentViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, errors, parentId) = await _adminService.CreateParentAsync(model, adminId);
        if (success)
        {
            TempData["SuccessMessage"] = model.StudentId.HasValue
                ? $"تم إنشاء حساب ولي الأمر ({model.FullName}) وربطه بالطالب بنجاح."
                : $"تم إنشاء حساب ولي الأمر ({model.FullName}) بنجاح.";
            return RedirectToAction(nameof(ParentDetails), new { id = parentId });
        }

        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ParentDetails(int id, int? studentId = null)
    {
        var model = await _adminService.GetParentDetailsAsync(id, studentId);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkStudent(int parentId, int studentId)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, message) = await _adminService.LinkStudentAsync(parentId, studentId, adminId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(ParentDetails), new { id = parentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkStudent(int parentId, int studentId)
    {
        var (success, message) = await _adminService.UnlinkStudentAsync(parentId, studentId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(ParentDetails), new { id = parentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleParentStatus(int id)
    {
        var (success, message) = await _adminService.ToggleParentStatusAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Parents));
    }

    [HttpGet]
    public async Task<IActionResult> Payments(PaymentStatus? status = null)
    {
        ViewBag.SelectedStatus = status;
        var payments = await _adminService.GetPaymentRequestsAsync(status);
        return View(payments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovePayment(int id)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, message) = await _adminService.ApprovePaymentAsync(id, adminId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Payments), new { status = PaymentStatus.Pending });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPayment(int id, string rejectionReason)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, message) = await _adminService.RejectPaymentAsync(id, adminId, rejectionReason);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Payments), new { status = PaymentStatus.Pending });
    }
    [HttpGet]
    public async Task<IActionResult> PendingUsers()
    {
        var users = await _adminService.GetPendingUsersAsync();
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveUser(int id)
    {
        var (success, message) = await _adminService.ApproveUserAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(PendingUsers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectUser(int id)
    {
        var (success, message) = await _adminService.RejectUserAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(PendingUsers));
    }
}
