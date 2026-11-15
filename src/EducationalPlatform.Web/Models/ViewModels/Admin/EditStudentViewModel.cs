using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.Web.Models.ViewModels.Admin;

public class EditStudentViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الطالب بالكامل مطلوب")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم هاتف الطالب مطلوب")]
    [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "رقم الهاتف غير صحيح (مثال: 01012345678)")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم هاتف ولي الأمر مطلوب")]
    [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "رقم الهاتف غير صحيح (مثال: 01012345678)")]
    public string ParentPhoneNumber { get; set; } = string.Empty;
}
