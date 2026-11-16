using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.Web.ViewModels;

public class EditExamViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الامتحان مطلوب")]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Range(1, 300, ErrorMessage = "مدة الامتحان يجب أن تكون بين 1 و300 دقيقة")]
    public int DurationMinutes { get; set; }

    [Required]
    [Range(1, 20, ErrorMessage = "عدد المحاولات يجب أن يكون بين 1 و20")]
    public int MaxAttempts { get; set; }
}