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
