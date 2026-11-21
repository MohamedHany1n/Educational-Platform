using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Models.ViewModels.Student;
using EducationalPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationalPlatform.Web.Controllers;

/// <summary>FR-11/FR-16: student payment submission, own request list, and private receipt serving.</summary>
[Authorize]
public class PaymentsController : Controller
{
    private readonly IStudentService _studentService;
    private readonly IReceiptStorage _receiptStorage;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public PaymentsController(IStudentService studentService, IReceiptStorage receiptStorage, ApplicationDbContext context, IConfiguration configuration)
    {
        _studentService = studentService;
        _receiptStorage = receiptStorage;
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// True only when at least one real payment destination is configured.
    /// Asking a student to pay an unspecified destination would collect
    /// unverifiable receipts, so the flow blocks instead.
    /// </summary>
    private bool PaymentDestinationConfigured()
        => !string.IsNullOrWhiteSpace(_configuration["PaymentInfo:InstaPay"])
        || !string.IsNullOrWhiteSpace(_configuration["PaymentInfo:VodafoneCash"]);

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Create(int courseId)
    {
        var course = await _studentService.GetCourseDetailsAsync(courseId, UserId);
        if (course == null)
            return NotFound();
        if (course.IsEnrolled)
        {
            TempData["ErrorMessage"] = "أنت مشترك بالفعل في هذا الكورس.";
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }
        if (course.HasPendingRequest)
        {
            TempData["ErrorMessage"] = "لديك طلب دفع معلّق لهذا الكورس بالفعل.";
            return RedirectToAction(nameof(Mine));
        }
        if (!PaymentDestinationConfigured())
        {
            TempData["ErrorMessage"] = "الاشتراك غير متاح حالياً: لم تُعلن حسابات الدفع بعد.";
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        ViewBag.Course = course;
        return View(new SubmitPaymentViewModel { CourseId = courseId });
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubmitPaymentViewModel model, IFormFile? receipt)
    {
        var course = await _studentService.GetCourseDetailsAsync(model.CourseId, UserId);
        if (course == null)
            return NotFound();
        if (!PaymentDestinationConfigured())
        {
            TempData["ErrorMessage"] = "الاشتراك غير متاح حالياً: لم تُعلن حسابات الدفع بعد.";
            return RedirectToAction("Details", "Courses", new { id = model.CourseId });
        }

        if (!ModelState.IsValid || receipt == null || receipt.Length == 0)
        {
            if (receipt == null || receipt.Length == 0)
                ModelState.AddModelError(nameof(receipt), "صورة الإيصال مطلوبة.");
            ViewBag.Course = course;
            return View(model);
        }

        await using var stream = receipt.OpenReadStream();
        var (success, message) = await _studentService.SubmitPaymentRequestAsync(
            UserId, model.CourseId, model.PaymentMethod,
            model.TransactionNumber, model.SenderPhoneNumber,
            stream, receipt.FileName);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            ViewBag.Course = course;
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Mine));
    }

    [HttpGet]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Mine()
    {
        var requests = await _studentService.GetMyPaymentRequestsAsync(UserId);
        return View(requests);
    }

    /// <summary>
    /// Serves a stored receipt through authorization, never as a static file:
    /// the owning student or any admin can view it, nobody else.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        var receipt = await _context.PaymentRequests
            .Where(p => p.Id == id)
            .Select(p => new { p.StudentId, p.ReceiptImagePath })
            .FirstOrDefaultAsync();
        if (receipt == null)
            return NotFound();

        var isOwner = receipt.StudentId == UserId;
        var isAdmin = User.IsInRole("Admin");
        if (!isOwner && !isAdmin)
            return Forbid();

        var file = await _receiptStorage.OpenAsync(receipt.ReceiptImagePath);
        if (file == null)
            return NotFound();

        return File(file.Value.Content, file.Value.ContentType);
    }
}
