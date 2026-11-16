namespace EducationalPlatform.Web.Models.ViewModels.Admin;

public class PendingUserViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ParentPhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}