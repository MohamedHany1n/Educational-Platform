using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.Web.Models.ViewModels.Admin;

public class CreateTeacherViewModel
{
    [Required(ErrorMessage = "اسم المدرس بالكامل مطلوب")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "كلمة المرور المبدئية مطلوبة")]
    [MinLength(6, ErrorMessage = "كلمة المرور لا تقل عن 6 أحرف وأرقام")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
