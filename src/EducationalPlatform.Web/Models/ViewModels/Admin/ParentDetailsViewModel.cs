namespace EducationalPlatform.Web.Models.ViewModels.Admin;

public class ParentDetailsViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<LinkedChildViewModel> Children { get; set; } = new();

    /// <summary>Students that are not linked to this parent yet (for the link dropdown).</summary>
    public List<StudentOptionViewModel> AvailableStudents { get; set; } = new();

    public int? PreselectedStudentId { get; set; }
}

public class LinkedChildViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime LinkedAt { get; set; }
}

public class StudentOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}
