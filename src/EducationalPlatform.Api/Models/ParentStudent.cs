namespace EducationalPlatform.Api.Models;

/// <summary>Links a parent account to a student account (many-to-many).</summary>
public class ParentStudent
{
    public int ParentId { get; set; }
    public ApplicationUser Parent { get; set; } = null!;

    public int StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;
}
