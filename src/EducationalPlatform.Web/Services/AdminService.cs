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
    // ---------------- Parents ----------------

    public async Task<List<ParentListItemViewModel>> GetParentsAsync()
    {
        var parents = await _userManager.GetUsersInRoleAsync("Parent");
        var childrenMap = await _adminRepository.GetParentChildrenCountMapAsync();

        return parents
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ParentListItemViewModel
            {
                Id = p.Id,
                FullName = p.FullName,
                Email = p.Email ?? string.Empty,
                PhoneNumber = p.PhoneNumber,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                ChildrenCount = childrenMap.GetValueOrDefault(p.Id, 0)
            })
            .ToList();
    }

    /// <summary>
    /// Builds the "create parent" form. If a student is given, the form is pre-filled with the parent phone
    /// the student entered at registration. If a parent account with that phone already exists
    /// (for example a second child of the same parent), its id is returned so the admin links instead of creating.
    /// </summary>
    public async Task<(CreateParentViewModel Model, int? ExistingParentId)> PrepareCreateParentAsync(int? studentId)
    {
        var model = new CreateParentViewModel();
        if (studentId == null) return (model, null);

        var student = await _userManager.FindByIdAsync(studentId.Value.ToString());
        if (student == null || student.IsDeleted || !await _userManager.IsInRoleAsync(student, "Student"))
            return (model, null);

        model.StudentId = student.Id;
        model.StudentName = student.FullName;
        model.PhoneNumber = student.ParentPhoneNumber ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(student.ParentPhoneNumber))
        {
            var parents = await _userManager.GetUsersInRoleAsync("Parent");
            var existing = parents.FirstOrDefault(p => !p.IsDeleted && p.PhoneNumber == student.ParentPhoneNumber);
            if (existing != null) return (model, existing.Id);
        }

        return (model, null);
    }

    public async Task<(bool Success, IEnumerable<string> Errors, int? ParentId)> CreateParentAsync(
        CreateParentViewModel model, int adminId)
    {
        if (await _userManager.FindByEmailAsync(model.Email) != null)
            return (false, new[] { "هذا البريد الإلكتروني مستخدم بالفعل." }, null);

        var phone = model.PhoneNumber.Trim();
        if (_userManager.Users.Any(u => u.PhoneNumber == phone))
            return (false, new[] { "رقم الهاتف مسجل بالفعل لحساب آخر." }, null);

        ApplicationUser? student = null;
        if (model.StudentId.HasValue)
        {
            student = await _userManager.FindByIdAsync(model.StudentId.Value.ToString());
            if (student == null || student.IsDeleted || !await _userManager.IsInRoleAsync(student, "Student"))
                return (false, new[] { "حساب الطالب غير موجود." }, null);
        }

        var parent = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = phone,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(parent, model.Password);
        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description), null);

        await _userManager.AddToRoleAsync(parent, "Parent");

        if (student != null)
            await _adminRepository.AddLinkAsync(parent.Id, student.Id, adminId);

        return (true, Enumerable.Empty<string>(), parent.Id);
    }

    public async Task<ParentDetailsViewModel?> GetParentDetailsAsync(int parentId, int? preselectedStudentId = null)
    {
        var parent = await _userManager.FindByIdAsync(parentId.ToString());
        if (parent == null || parent.IsDeleted || !await _userManager.IsInRoleAsync(parent, "Parent"))
            return null;

        var links = await _adminRepository.GetLinksByParentAsync(parentId);
        var linkedIds = links.Select(l => l.StudentId).ToHashSet();

        var students = await _userManager.GetUsersInRoleAsync("Student");
        var available = students
            .Where(s => !s.IsDeleted && !linkedIds.Contains(s.Id))
            .OrderBy(s => s.FullName)
            .Select(s => new StudentOptionViewModel
            {
                Id = s.Id,
                Label = $"{s.FullName} - {s.PhoneNumber ?? s.Email}" + (s.IsActive ? "" : " (بانتظار التفعيل)")
            })
            .ToList();

        return new ParentDetailsViewModel
        {
            Id = parent.Id,
            FullName = parent.FullName,
            Email = parent.Email ?? string.Empty,
            PhoneNumber = parent.PhoneNumber,
            IsActive = parent.IsActive,
            CreatedAt = parent.CreatedAt,
            Children = links
                .Where(l => !l.Student.IsDeleted)
                .Select(l => new LinkedChildViewModel
                {
                    Id = l.StudentId,
                    FullName = l.Student.FullName,
                    Email = l.Student.Email ?? string.Empty,
                    PhoneNumber = l.Student.PhoneNumber,
                    IsActive = l.Student.IsActive,
                    LinkedAt = l.LinkedAt
                })
                .ToList(),
            AvailableStudents = available,
            PreselectedStudentId = preselectedStudentId
        };
    }

    public async Task<(bool Success, string Message)> LinkStudentAsync(int parentId, int studentId, int adminId)
    {
        var parent = await _userManager.FindByIdAsync(parentId.ToString());
        if (parent == null || parent.IsDeleted || !await _userManager.IsInRoleAsync(parent, "Parent"))
            return (false, "حساب ولي الأمر غير موجود.");

        var student = await _userManager.FindByIdAsync(studentId.ToString());
        if (student == null || student.IsDeleted || !await _userManager.IsInRoleAsync(student, "Student"))
            return (false, "حساب الطالب غير موجود.");

        if (await _adminRepository.LinkExistsAsync(parentId, studentId))
            return (false, "هذا الطالب مرتبط بولي الأمر بالفعل.");

        await _adminRepository.AddLinkAsync(parentId, studentId, adminId);
        return (true, $"تم ربط الطالب ({student.FullName}) بولي الأمر ({parent.FullName}).");
    }

    public async Task<(bool Success, string Message)> UnlinkStudentAsync(int parentId, int studentId)
    {
        var removed = await _adminRepository.RemoveLinkAsync(parentId, studentId);
        return removed
            ? (true, "تم فك الربط بين الطالب وولي الأمر.")
            : (false, "الربط غير موجود.");
    }

    public async Task<(bool Success, string Message)> ToggleParentStatusAsync(int parentId)
    {
        var user = await _userManager.FindByIdAsync(parentId.ToString());
        if (user == null || user.IsDeleted || !await _userManager.IsInRoleAsync(user, "Parent"))
            return (false, "حساب ولي الأمر غير موجود.");

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var stateText = user.IsActive ? "تفعيل" : "إيقاف";
        return (true, $"تم {stateText} حساب ولي الأمر ({user.FullName}) بنجاح.");
    }

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
                PhoneNumber = u.PhoneNumber,
                ParentPhoneNumber = u.ParentPhoneNumber,
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
                PhoneNumber = u.PhoneNumber,
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

        if (!await IsPendingRegistrationAsync(user))
            return (false, "هذا الحساب ليس طلب تسجيل في الانتظار.");

        user.IsActive = true;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, "تعذّر تفعيل الحساب.");

        return (true, $"تم تفعيل حساب ({user.FullName}) بنجاح.");
    }

    public async Task<(bool Success, string Message)> RejectUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, "الحساب غير موجود.");

        if (!await IsPendingRegistrationAsync(user))
            return (false, "هذا الحساب ليس طلب تسجيل في الانتظار.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return (false, "تعذّر حذف طلب التسجيل.");

        return (true, $"تم رفض وحذف طلب حساب ({user.FullName}).");
    }

    // Only inactive Student or Parent accounts are registration requests.
    // This stops the admin actions from touching teachers, admins or active users.
    private async Task<bool> IsPendingRegistrationAsync(ApplicationUser user)
    {
        if (user.IsActive) return false;

        return await _userManager.IsInRoleAsync(user, "Student")
            || await _userManager.IsInRoleAsync(user, "Parent");
    }
}
