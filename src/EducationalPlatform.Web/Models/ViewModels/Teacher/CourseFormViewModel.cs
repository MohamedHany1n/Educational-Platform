using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.Web.Models.ViewModels.Teacher;

public class CourseFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الكورس مطلوب")]
    [StringLength(150, ErrorMessage = "العنوان لا يزيد عن 150 حرف")]
    [Display(Name = "عنوان الكورس")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "الوصف لا يزيد عن 2000 حرف")]
    [Display(Name = "الوصف")]
    public string? Description { get; set; }

    [Range(0, 100000, ErrorMessage = "السعر غير صحيح")]
    [Display(Name = "السعر")]
    public decimal Price { get; set; }

    [Display(Name = "منشور")]
    public bool IsPublished { get; set; }

    [Display(Name = "صورة الكورس")]
    public IFormFile? Thumbnail { get; set; }

    public string? ThumbnailPath { get; set; }
}
