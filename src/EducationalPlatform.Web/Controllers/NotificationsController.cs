using EducationalPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EducationalPlatform.Web.Controllers;

/// <summary>In-app notifications for any logged-in user.</summary>
[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var notifications = await _notificationService.GetForUserAsync(UserId);
        await _notificationService.MarkAllReadAsync(UserId);
        return View(notifications);
    }
}
