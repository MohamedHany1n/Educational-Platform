namespace EducationalPlatform.Web.Models.ViewModels.Admin;

public class StudentListViewModel
{
    public List<StudentListItemViewModel> Items { get; set; } = new();
    public string? Search { get; set; }

    /// <summary>"" (all, without deleted), active, pending, suspended, deleted.</summary>
    public string Status { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class StudentListItemViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ParentPhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int EnrollmentsCount { get; set; }
    public int CompletedLessons { get; set; }

    public string StatusKey => IsDeleted ? "deleted"
        : IsActive ? "active"
        : ApprovedAt == null ? "pending"
        : "suspended";
}
