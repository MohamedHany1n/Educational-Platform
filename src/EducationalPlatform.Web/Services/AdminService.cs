using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Models.ViewModels.Admin;
using EducationalPlatform.Web.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
        var recentPending = await _adminRepository.GetRecentPaymentRequestsAsync(PaymentStatus.Pending, 5);

        return new AdminDashboardViewModel
        {
            TotalTeachers = teachers.Count(t => t.IsActive && !t.IsDeleted),
            TotalStudents = students.Count(s => s.IsActive && !s.IsDeleted),
            TotalCourses = await _adminRepository.GetCoursesCountAsync(),
            PendingPaymentsCount = await _adminRepository.GetPendingPaymentsCountAsync(),
            TotalApprovedRevenue = await _adminRepository.GetTotalApprovedRevenueAsync(),
            RecentPendingPayments = recentPending
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

        var phone = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        if (phone != null && await _userManager.Users.AnyAsync(u => u.PhoneNumber == phone))
        {
            return (false, new[] { "رقم الهاتف مسجل بالفعل لحساب آخر." });
        }

        var teacher = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = phone,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        IdentityResult result;
        try
        {
            result = await _userManager.CreateAsync(teacher, model.Password);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return (false, new[] { "رقم الهاتف مسجل بالفعل لحساب آخر." });
        }

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

        IdentityResult result;
        try
        {
            result = await _userManager.CreateAsync(parent, model.Password);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return (false, new[] { "رقم الهاتف مسجل بالفعل لحساب آخر." }, null);
        }

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

    // ---------------- Students ----------------

    private const int StudentsPageSize = 15;

    public async Task<StudentListViewModel> GetStudentsAsync(string? search, string? status, int page)
    {
        if (page < 1) page = 1;

        var (items, total) = await _adminRepository.GetStudentsAsync(search, status, page, StudentsPageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)StudentsPageSize));

        return new StudentListViewModel
        {
            Items = items,
            Search = search,
            Status = status ?? string.Empty,
            Page = page,
            TotalPages = totalPages,
            TotalCount = total
        };
    }

    public async Task<StudentDetailsViewModel?> GetStudentDetailsAsync(int studentId)
        => await _adminRepository.GetStudentDetailsAsync(studentId);

    public async Task<EditStudentViewModel?> GetEditStudentAsync(int studentId)
    {
        var student = await GetStudentUserAsync(studentId);
        if (student == null || student.IsDeleted) return null;

        return new EditStudentViewModel
        {
            Id = student.Id,
            FullName = student.FullName,
            Email = student.Email ?? string.Empty,
            PhoneNumber = student.PhoneNumber ?? string.Empty,
            ParentPhoneNumber = student.ParentPhoneNumber ?? string.Empty
        };
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateStudentAsync(EditStudentViewModel model)
    {
        var student = await GetStudentUserAsync(model.Id);
        if (student == null || student.IsDeleted)
            return (false, new[] { "حساب الطالب غير موجود." });

        var phone = model.PhoneNumber.Trim();
        var parentPhone = model.ParentPhoneNumber.Trim();

        if (phone == parentPhone)
            return (false, new[] { "رقم ولي الأمر يجب أن يختلف عن رقم الطالب." });

        if (_userManager.Users.Any(u => u.Id != student.Id && u.PhoneNumber == phone))
            return (false, new[] { "رقم الهاتف مسجل بالفعل لحساب آخر." });

        var emailOwner = await _userManager.FindByEmailAsync(model.Email);
        if (emailOwner != null && emailOwner.Id != student.Id)
            return (false, new[] { "هذا البريد الإلكتروني مستخدم بالفعل." });

        if (!string.Equals(student.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _userManager.SetEmailAsync(student, model.Email);
            if (!emailResult.Succeeded)
                return (false, emailResult.Errors.Select(e => e.Description));

            var nameResult = await _userManager.SetUserNameAsync(student, model.Email);
            if (!nameResult.Succeeded)
                return (false, nameResult.Errors.Select(e => e.Description));
        }

        student.FullName = model.FullName.Trim();
        student.PhoneNumber = phone;
        student.ParentPhoneNumber = parentPhone;

        IdentityResult result;
        try
        {
            result = await _userManager.UpdateAsync(student);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return (false, new[] { "رقم الهاتف مسجل بالفعل لحساب آخر." });
        }

        return result.Succeeded
            ? (true, Enumerable.Empty<string>())
            : (false, result.Errors.Select(e => e.Description));
    }

    /// <summary>Suspend or re-activate an approved student. Pending registrations are handled by approve / reject.</summary>
    public async Task<(bool Success, string Message)> ToggleStudentStatusAsync(int studentId)
    {
        var student = await GetStudentUserAsync(studentId);
        if (student == null || student.IsDeleted)
            return (false, "حساب الطالب غير موجود.");

        if (!student.IsActive && student.ApprovedAt == null)
            return (false, "هذا الطالب ما زال في انتظار الموافقة، فعّله من صفحة طلبات الحسابات.");

        if (student.IsActive)
        {
            // Remember that this account was approved before, so it never goes back to the pending list.
            student.ApprovedAt ??= DateTime.UtcNow;
            student.IsActive = false;
        }
        else
        {
            student.IsActive = true;
        }

        await _userManager.UpdateAsync(student);

        var stateText = student.IsActive ? "تفعيل" : "إيقاف";
        return (true, $"تم {stateText} حساب الطالب ({student.FullName}) بنجاح.");
    }

    /// <summary>Soft delete: the student disappears from the lists and cannot log in, but payments, progress and grades are kept.</summary>
    public async Task<(bool Success, string Message)> DeleteStudentAsync(int studentId)
    {
        var student = await GetStudentUserAsync(studentId);
        if (student == null || student.IsDeleted)
            return (false, "حساب الطالب غير موجود.");

        student.ApprovedAt ??= DateTime.UtcNow;
        student.IsActive = false;
        student.IsDeleted = true;
        await _userManager.UpdateAsync(student);

        return (true, $"تم حذف الطالب ({student.FullName}). يمكنك استرجاعه من فلتر المحذوفين.");
    }

    /// <summary>Restores a deleted student as suspended, so the admin decides when to activate the account again.</summary>
    public async Task<(bool Success, string Message)> RestoreStudentAsync(int studentId)
    {
        var student = await GetStudentUserAsync(studentId);
        if (student == null || !student.IsDeleted)
            return (false, "حساب الطالب غير موجود.");

        student.IsDeleted = false;
        student.IsActive = false;
        await _userManager.UpdateAsync(student);

        return (true, $"تم استرجاع الطالب ({student.FullName}) وهو موقوف حالياً، فعّل الحساب عندما تريد.");
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 2601 or 2627 };

    private async Task<ApplicationUser?> GetStudentUserAsync(int studentId)
    {
        var user = await _userManager.FindByIdAsync(studentId.ToString());
        if (user == null || !await _userManager.IsInRoleAsync(user, "Student"))
            return null;
        return user;
    }

    // ---------------- Pending registrations ----------------

    // Only students register themselves. A pending registration is an inactive student that was never approved.
    public async Task<List<PendingUserViewModel>> GetPendingUsersAsync()
    {
        var students = await _userManager.GetUsersInRoleAsync("Student");

        return students
            .Where(u => !u.IsActive && !u.IsDeleted && u.ApprovedAt == null)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new PendingUserViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = "Student",
                PhoneNumber = u.PhoneNumber,
                ParentPhoneNumber = u.ParentPhoneNumber,
                CreatedAt = u.CreatedAt
            })
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
        user.ApprovedAt = DateTime.UtcNow;
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

        // A parent may already be linked to this pending student: remove the links first.
        await _adminRepository.RemoveLinksByStudentAsync(user.Id);

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return (false, "تعذّر حذف طلب التسجيل.");

        return (true, $"تم رفض وحذف طلب حساب ({user.FullName}).");
    }

    // Only inactive, never-approved students are registration requests.
    // This stops the admin actions from touching teachers, parents, suspended or deleted accounts.
    private async Task<bool> IsPendingRegistrationAsync(ApplicationUser user)
    {
        if (user.IsActive || user.IsDeleted || user.ApprovedAt != null) return false;

        return await _userManager.IsInRoleAsync(user, "Student");
    }
}
