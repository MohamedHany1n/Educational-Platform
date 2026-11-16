using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Models.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Web.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly ApplicationDbContext _context;

    public AdminRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetCoursesCountAsync()
        => await _context.Courses.CountAsync();

    public async Task<int> GetPendingPaymentsCountAsync()
        => await _context.PaymentRequests.CountAsync(p => p.Status == PaymentStatus.Pending);

    public async Task<decimal> GetTotalApprovedRevenueAsync()
        => await _context.PaymentRequests
            .Where(p => p.Status == PaymentStatus.Approved)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

    public async Task<Dictionary<int, int>> GetTeacherCoursesCountMapAsync()
        => await _context.Courses
            .GroupBy(c => c.TeacherId)
            .Select(g => new { TeacherId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TeacherId, x => x.Count);

    public async Task<Dictionary<int, int>> GetParentChildrenCountMapAsync()
        => await _context.ParentStudents
            .GroupBy(ps => ps.ParentId)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParentId, x => x.Count);

    public async Task<List<ParentStudent>> GetLinksByParentAsync(int parentId)
        => await _context.ParentStudents
            .Include(ps => ps.Student)
            .Where(ps => ps.ParentId == parentId)
            .OrderBy(ps => ps.LinkedAt)
            .ToListAsync();

    public async Task<bool> LinkExistsAsync(int parentId, int studentId)
        => await _context.ParentStudents
            .AnyAsync(ps => ps.ParentId == parentId && ps.StudentId == studentId);

    public async Task AddLinkAsync(int parentId, int studentId, int adminId)
    {
        _context.ParentStudents.Add(new ParentStudent
        {
            ParentId = parentId,
            StudentId = studentId,
            LinkedByAdminId = adminId,
            LinkedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public async Task<bool> RemoveLinkAsync(int parentId, int studentId)
    {
        var link = await _context.ParentStudents
            .FirstOrDefaultAsync(ps => ps.ParentId == parentId && ps.StudentId == studentId);
        if (link == null) return false;

        _context.ParentStudents.Remove(link);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task RemoveLinksByStudentAsync(int studentId)
    {
        var links = await _context.ParentStudents.Where(ps => ps.StudentId == studentId).ToListAsync();
        if (links.Count == 0) return;

        _context.ParentStudents.RemoveRange(links);
        await _context.SaveChangesAsync();
    }

    // ---------------- Students ----------------

    /// <summary>All users that have the Student role (deleted ones included, callers filter).</summary>
    private IQueryable<ApplicationUser> StudentsQuery()
        => from u in _context.Users
           join ur in _context.UserRoles on u.Id equals ur.UserId
           join r in _context.Roles on ur.RoleId equals r.Id
           where r.Name == "Student"
           select u;

    public async Task<(List<StudentListItemViewModel> Items, int TotalCount)> GetStudentsAsync(
        string? search, string? status, int page, int pageSize)
    {
        var query = StudentsQuery();

        query = status switch
        {
            "deleted" => query.Where(u => u.IsDeleted),
            "active" => query.Where(u => !u.IsDeleted && u.IsActive),
            "pending" => query.Where(u => !u.IsDeleted && !u.IsActive && u.ApprovedAt == null),
            "suspended" => query.Where(u => !u.IsDeleted && !u.IsActive && u.ApprovedAt != null),
            _ => query.Where(u => !u.IsDeleted)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(s) ||
                (u.Email != null && u.Email.Contains(s)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(s)) ||
                (u.ParentPhoneNumber != null && u.ParentPhoneNumber.Contains(s)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new StudentListItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                ParentPhoneNumber = u.ParentPhoneNumber,
                IsActive = u.IsActive,
                IsDeleted = u.IsDeleted,
                ApprovedAt = u.ApprovedAt,
                CreatedAt = u.CreatedAt,
                EnrollmentsCount = u.Enrollments.Count(),
                CompletedLessons = u.LessonProgress.Count(lp => lp.IsCompleted)
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task<StudentDetailsViewModel?> GetStudentDetailsAsync(int studentId)
    {
        var student = await StudentsQuery().FirstOrDefaultAsync(u => u.Id == studentId);
        if (student == null) return null;

        var courses = await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new StudentCourseProgressViewModel
            {
                CourseId = e.CourseId,
                CourseTitle = e.Course.Title,
                TeacherName = e.Course.Teacher.FullName,
                EnrolledAt = e.EnrolledAt,
                TotalLessons = e.Course.Lessons.Count(),
                CompletedLessons = e.Student.LessonProgress
                    .Count(lp => lp.IsCompleted && lp.Lesson.CourseId == e.CourseId)
            })
            .ToListAsync();

        var exams = await _context.ExamAttempts
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.StartedAt)
            .Select(a => new StudentExamResultViewModel
            {
                AttemptId = a.Id,
                ExamTitle = a.Exam.Title,
                CourseTitle = a.Exam.Course.Title,
                Status = a.Status,
                TotalScore = a.TotalScore,
                MaxScore = a.Exam.Questions.Sum(q => q.Marks),
                StartedAt = a.StartedAt,
                SubmittedAt = a.SubmittedAt
            })
            .ToListAsync();

        var payments = await _context.PaymentRequests
            .Where(p => p.StudentId == studentId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new StudentPaymentViewModel
            {
                Id = p.Id,
                CourseTitle = p.Course.Title,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                TransactionNumber = p.TransactionNumber,
                Status = p.Status,
                RejectionReason = p.RejectionReason,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var parents = await _context.ParentStudents
            .Where(ps => ps.StudentId == studentId)
            .OrderBy(ps => ps.LinkedAt)
            .Select(ps => new StudentParentViewModel
            {
                Id = ps.ParentId,
                FullName = ps.Parent.FullName,
                PhoneNumber = ps.Parent.PhoneNumber,
                LinkedAt = ps.LinkedAt
            })
            .ToListAsync();

        return new StudentDetailsViewModel
        {
            Id = student.Id,
            FullName = student.FullName,
            Email = student.Email ?? string.Empty,
            PhoneNumber = student.PhoneNumber,
            ParentPhoneNumber = student.ParentPhoneNumber,
            IsActive = student.IsActive,
            IsDeleted = student.IsDeleted,
            ApprovedAt = student.ApprovedAt,
            CreatedAt = student.CreatedAt,
            Courses = courses,
            Exams = exams,
            Payments = payments,
            Parents = parents
        };
    }

    public async Task<List<PaymentRequest>> GetPaymentRequestsAsync(PaymentStatus? status = null)
    {
        var query = _context.PaymentRequests
            .Include(p => p.Student)
            .Include(p => p.Course)
            .Include(p => p.ReviewedByAdmin)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<PaymentRequest?> GetPaymentRequestByIdAsync(int id)
        => await _context.PaymentRequests
            .Include(p => p.Student)
            .Include(p => p.Course)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<(bool Success, string Message)> ApprovePaymentAsync(int paymentRequestId, int adminId)
    {
        var request = await _context.PaymentRequests.FindAsync(paymentRequestId);
        if (request == null)
            return (false, "طلب الدفع غير موجود.");

        if (request.Status != PaymentStatus.Pending)
            return (false, "تمت مراجعة هذا الطلب مسبقاً.");

        request.Status = PaymentStatus.Approved;
        request.ReviewedByAdminId = adminId;
        request.ReviewedAt = DateTime.UtcNow;
        request.RejectionReason = null;

        var alreadyEnrolled = await _context.Enrollments
            .AnyAsync(e => e.StudentId == request.StudentId && e.CourseId == request.CourseId);

        if (!alreadyEnrolled)
        {
            var enrollment = new Enrollment
            {
                StudentId = request.StudentId,
                CourseId = request.CourseId,
                PaymentRequestId = request.Id,
                EnrolledAt = DateTime.UtcNow
            };
            await _context.Enrollments.AddAsync(enrollment);
        }

        await _context.SaveChangesAsync();
        return (true, "تم قبول الإيصال وتفعيل اشتراك الطالب في الكورس بنجاح.");
    }

    public async Task<(bool Success, string Message)> RejectPaymentAsync(int paymentRequestId, int adminId, string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            return (false, "يجب كتابة سبب رفض الإيصال.");

        var request = await _context.PaymentRequests.FindAsync(paymentRequestId);
        if (request == null)
            return (false, "طلب الدفع غير موجود.");

        if (request.Status != PaymentStatus.Pending)
            return (false, "تمت مراجعة هذا الطلب مسبقاً.");

        request.Status = PaymentStatus.Rejected;
        request.ReviewedByAdminId = adminId;
        request.ReviewedAt = DateTime.UtcNow;
        request.RejectionReason = rejectionReason.Trim();

        await _context.SaveChangesAsync();
        return (true, "تم رفض طلب الدفع وتسجيل السبب.");
    }
}
