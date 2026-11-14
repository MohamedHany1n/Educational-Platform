using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Models.ViewModels.Admin;

namespace EducationalPlatform.Web.Services;

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardStatsAsync();
    Task<List<TeacherListItemViewModel>> GetTeachersAsync();
    Task<(bool Success, IEnumerable<string> Errors)> CreateTeacherAsync(CreateTeacherViewModel model);
    Task<(bool Success, string Message)> ToggleTeacherStatusAsync(int teacherId);
    Task<List<PaymentRequest>> GetPaymentRequestsAsync(PaymentStatus? status = null);
    Task<(bool Success, string Message)> ApprovePaymentAsync(int paymentRequestId, int adminId);
    Task<(bool Success, string Message)> RejectPaymentAsync(int paymentRequestId, int adminId, string rejectionReason);
    Task<List<PendingUserViewModel>> GetPendingUsersAsync();
    Task<(bool Success, string Message)> ApproveUserAsync(int userId);
    Task<(bool Success, string Message)> RejectUserAsync(int userId);
}
