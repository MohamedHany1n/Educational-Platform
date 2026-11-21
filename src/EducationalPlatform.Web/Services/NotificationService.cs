using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Web.Services;

public class NotificationViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Url { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface INotificationService
{
    Task<List<NotificationViewModel>> GetForUserAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAllReadAsync(int userId);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationViewModel>> GetForUserAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationViewModel
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                Url = n.Url,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
        => await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAllReadAsync(int userId)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
