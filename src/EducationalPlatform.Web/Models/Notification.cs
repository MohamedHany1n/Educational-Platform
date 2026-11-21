namespace EducationalPlatform.Web.Models;

/// <summary>An in-app notification for one user, for example a payment review result.</summary>
public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>Optional link to the page the notification is about.</summary>
    public string? Url { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
