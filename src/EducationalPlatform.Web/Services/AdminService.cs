using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Models.ViewModels.Admin;
using EducationalPlatform.Web.Repositories;
using Microsoft.AspNetCore.Identity;

namespace EducationalPlatform.Web.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _adminRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(
        IAdminRepository adminRepository,
        UserManager<ApplicationUser> userManager)
    {
        _adminRepository = adminRepository;
        _userManager = userManager;
    }

    public async Task<AdminDashboardViewModel> GetDashboardStatsAsync()
    {
        var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
        var students = await _userManager.GetUsersInRoleAsync("Student");
        var pendingPayments = await _adminRepository.GetPaymentRequestsAsync(PaymentStatus.Pending);

        return new AdminDashboardViewModel
        {
            TotalTeachers = teachers.Count,
            TotalStudents = students.Count,
            TotalCourses = await _adminRepository.GetCoursesCountAsync(),
            PendingPaymentsCount = pendingPayments.Count,
            TotalApprovedRevenue = await _adminRepository.GetTotalApprovedRevenueAsync(),
            RecentPendingPayments = pendingPayments.Take(5).ToList()
        };
    }

    public async Task<List<TeacherListItemViewModel>> GetTeachersAsync()
    {
        var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
        var coursesMap = await _adminRepository.GetTeacherCoursesCountMapAsync();

        return teachers
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TeacherListItemViewModel
            {
                Id = t.Id,
                FullName = t.FullName,
                Email = t.Email ?? string.Empty,
                PhoneNumber = t.PhoneNumber,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                CoursesCount = coursesMap.GetValueOrDefault(t.Id, 0)
            })
            .ToList();
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateTeacherAsync(CreateTeacherViewModel model)
    {
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return (false, new[] { "هذا البريد الإلكتروني مستخدم بالفعل." });
        }

        var teacher = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(teacher, model.Password);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        await _userManager.AddToRoleAsync(teacher, "Teacher");
        return (true, Enumerable.Empty<string>());
    }

    public async Task<(bool Success, string Message)> ToggleTeacherStatusAsync(int teacherId)
    {
        var user = await _userManager.FindByIdAsync(teacherId.ToString());
        if (user == null || !await _userManager.IsInRoleAsync(user, "Teacher"))
        {
            return (false, "حساب المدرس غير موجود.");
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var stateText = user.IsActive ? "تفعيل" : "إيقاف";
        return (true, $"تم {stateText} حساب المدرس ({user.FullName}) بنجاح.");
    }

    public async Task<List<PaymentRequest>> GetPaymentRequestsAsync(PaymentStatus? status = null)
        => await _adminRepository.GetPaymentRequestsAsync(status);

    public async Task<(bool Success, string Message)> ApprovePaymentAsync(int paymentRequestId, int adminId)
        => await _adminRepository.ApprovePaymentAsync(paymentRequestId, adminId);

    public async Task<(bool Success, string Message)> RejectPaymentAsync(int paymentRequestId, int adminId, string rejectionReason)
        => await _adminRepository.RejectPaymentAsync(paymentRequestId, adminId, rejectionReason);
    public async Task<List<PendingUserViewModel>> GetPendingUsersAsync()
    {
        var students = await _userManager.GetUsersInRoleAsync("Student");
        var parents = await _userManager.GetUsersInRoleAsync("Parent");

        var pendingStudents = students
            .Where(u => !u.IsActive)
            .Select(u => new PendingUserViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = "Student",
                CreatedAt = u.CreatedAt
            });

        var pendingParents = parents
            .Where(u => !u.IsActive)
            .Select(u => new PendingUserViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = "Parent",
                CreatedAt = u.CreatedAt
            });

        return pendingStudents
            .Concat(pendingParents)
            .OrderByDescending(u => u.CreatedAt)
            .ToList();
    }

    public async Task<(bool Success, string Message)> ApproveUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, "الحساب غير موجود.");

        user.IsActive = true;
        await _userManager.UpdateAsync(user);
        return (true, $"تم تفعيل حساب ({user.FullName}) بنجاح.");
    }

    public async Task<(bool Success, string Message)> RejectUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, "الحساب غير موجود.");

        await _userManager.DeleteAsync(user);
        return (true, $"تم رفض وحذف طلب حساب ({user.FullName}).");
    }
}
