using EducationalPlatform.Web.Models;

namespace EducationalPlatform.Web.Models.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalTeachers { get; set; }
    public int TotalStudents { get; set; }
    public int TotalCourses { get; set; }
    public int PendingPaymentsCount { get; set; }
    public decimal TotalApprovedRevenue { get; set; }
    public List<PaymentRequest> RecentPendingPayments { get; set; } = new();
}
