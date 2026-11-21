using EducationalPlatform.Web.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.Web.Models.ViewModels.Student;

/// <summary>FR-11: the enrollment payment form.</summary>
public class SubmitPaymentViewModel
{
    [Required]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "اختر طريقة الدفع")]
    public PaymentMethod PaymentMethod { get; set; }

    [Required(ErrorMessage = "رقم العملية مطلوب")]
    [MaxLength(50, ErrorMessage = "رقم العملية طويل جداً (الحد الأقصى 50 حرف)")]
    [Display(Name = "رقم العملية")]
    public string TransactionNumber { get; set; } = string.Empty;

    [MaxLength(15, ErrorMessage = "رقم الهاتف طويل جداً")]
    [Display(Name = "رقم المحفظة المرسل منها (اختياري)")]
    public string? SenderPhoneNumber { get; set; }
}
