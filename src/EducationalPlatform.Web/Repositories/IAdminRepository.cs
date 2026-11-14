using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;

namespace EducationalPlatform.Web.Repositories;

public interface IAdminRepository
{
    Task<int> GetCoursesCountAsync();
    Task<int> GetPendingPaymentsCountAsync();
    Task<decimal> GetTotalApprovedRevenueAsync();
    Task<Dictionary<int, int>> GetTeacherCoursesCountMapAsync();
    Task<List<PaymentRequest>> GetPaymentRequestsAsync(PaymentStatus? status = null);
    Task<PaymentRequest?> GetPaymentRequestByIdAsync(int id);
    Task<(bool Success, string Message)> ApprovePaymentAsync(int paymentRequestId, int adminId);
    Task<(bool Success, string Message)> RejectPaymentAsync(int paymentRequestId, int adminId, string rejectionReason);
}
