using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;

namespace EducationalPlatform.Web.Repositories;

public interface IAdminRepository
{
    Task<int> GetCoursesCountAsync();
    Task<int> GetPendingPaymentsCountAsync();
    Task<decimal> GetTotalApprovedRevenueAsync();
    Task<Dictionary<int, int>> GetTeacherCoursesCountMapAsync();
    Task<Dictionary<int, int>> GetParentChildrenCountMapAsync();
    Task<List<ParentStudent>> GetLinksByParentAsync(int parentId);
    Task<bool> LinkExistsAsync(int parentId, int studentId);
    Task AddLinkAsync(int parentId, int studentId, int adminId);
    Task<bool> RemoveLinkAsync(int parentId, int studentId);
    Task<List<PaymentRequest>> GetPaymentRequestsAsync(PaymentStatus? status = null);
    Task<PaymentRequest?> GetPaymentRequestByIdAsync(int id);
    Task<(bool Success, string Message)> ApprovePaymentAsync(int paymentRequestId, int adminId);
    Task<(bool Success, string Message)> RejectPaymentAsync(int paymentRequestId, int adminId, string rejectionReason);
}
