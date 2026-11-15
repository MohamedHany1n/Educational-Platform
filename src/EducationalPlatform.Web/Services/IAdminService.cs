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
    Task<List<ParentListItemViewModel>> GetParentsAsync();
    Task<(CreateParentViewModel Model, int? ExistingParentId)> PrepareCreateParentAsync(int? studentId);
    Task<(bool Success, IEnumerable<string> Errors, int? ParentId)> CreateParentAsync(CreateParentViewModel model, int adminId);
    Task<ParentDetailsViewModel?> GetParentDetailsAsync(int parentId, int? preselectedStudentId = null);
    Task<(bool Success, string Message)> LinkStudentAsync(int parentId, int studentId, int adminId);
    Task<(bool Success, string Message)> UnlinkStudentAsync(int parentId, int studentId);
    Task<(bool Success, string Message)> ToggleParentStatusAsync(int parentId);
    Task<List<PendingUserViewModel>> GetPendingUsersAsync();
    Task<(bool Success, string Message)> ApproveUserAsync(int userId);
    Task<(bool Success, string Message)> RejectUserAsync(int userId);
}
