using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
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
