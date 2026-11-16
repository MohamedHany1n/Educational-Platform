using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.Web.Models.ViewModels.Teacher;

public class LessonFormViewModel
{
    public int Id { get; set; }
    public int CourseId { get; set; }

    [Required(ErrorMessage = "عنوان الدرس مطلوب")]
    [StringLength(150, ErrorMessage = "العنوان لا يزيد عن 150 حرف")]
    [Display(Name = "عنوان الدرس")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "الوصف لا يزيد عن 2000 حرف")]
    [Display(Name = "وصف الدرس")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "رابط الفيديو مطلوب")]
    [StringLength(400, ErrorMessage = "الرابط لا يزيد عن 400 حرف")]
    [Display(Name = "رابط الفيديو")]
    public string VideoUrl { get; set; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "الترتيب لازم يكون من 1 إلى 1000")]
    [Display(Name = "الترتيب")]
    public int OrderIndex { get; set; } = 1;
}
