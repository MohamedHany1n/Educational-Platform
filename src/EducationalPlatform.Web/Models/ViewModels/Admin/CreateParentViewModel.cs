using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.Web.Models.ViewModels.Admin;

public class CreateParentViewModel
{
    [Required(ErrorMessage = "اسم ولي الأمر بالكامل مطلوب")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم هاتف ولي الأمر مطلوب")]
    [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "رقم الهاتف غير صحيح (مثال: 01012345678)")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور المبدئية مطلوبة")]
    [MinLength(6, ErrorMessage = "كلمة المرور لا تقل عن 6 أحرف وأرقام")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Optional: the student to link right after the parent account is created.</summary>
    public int? StudentId { get; set; }

    /// <summary>Display only (shown on the form when a student is selected).</summary>
    public string? StudentName { get; set; }
}
