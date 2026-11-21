using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Models.ViewModels.Student;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Web.Services;

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _context;
    private readonly IReceiptStorage _receiptStorage;

    public StudentService(ApplicationDbContext context, IReceiptStorage receiptStorage)
    {
        _context = context;
        _receiptStorage = receiptStorage;
    }

    public async Task<List<CourseCardViewModel>> GetPublishedCoursesAsync(string? search, int studentId)
    {
        var query = _context.Courses.Where(c => c.IsPublished);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Title.Contains(term));
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CourseCardViewModel
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Price = c.Price,
                TeacherName = c.Teacher.FullName,
                LessonsCount = c.Lessons.Count,
                IsEnrolled = c.Enrollments.Any(e => e.StudentId == studentId),
                HasPendingRequest = c.PaymentRequests.Any(p => p.StudentId == studentId && p.Status == PaymentStatus.Pending)
            })
            .ToListAsync();
    }

    public async Task<CourseDetailsViewModel?> GetCourseDetailsAsync(int courseId, int studentId)
    {
        return await _context.Courses
            .Where(c => c.Id == courseId && c.IsPublished)
            .Select(c => new CourseDetailsViewModel
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Price = c.Price,
                TeacherName = c.Teacher.FullName,
                LessonsCount = c.Lessons.Count,
                IsEnrolled = c.Enrollments.Any(e => e.StudentId == studentId),
                HasPendingRequest = c.PaymentRequests.Any(p => p.StudentId == studentId && p.Status == PaymentStatus.Pending)
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> SubmitPaymentRequestAsync(
        int studentId, int courseId, PaymentMethod method,
        string transactionNumber, string? senderPhoneNumber,
        Stream receiptContent, string receiptFileName)
    {
        var course = await _context.Courses
            .Where(c => c.Id == courseId && c.IsPublished)
            .Select(c => new { c.Id, c.Price, c.Title })
            .FirstOrDefaultAsync();
        if (course == null)
            return (false, "الكورس غير موجود أو غير منشور.");

        if (string.IsNullOrWhiteSpace(transactionNumber))
            return (false, "رقم العملية مطلوب.");

        var trimmedTransaction = transactionNumber.Trim();
        if (trimmedTransaction.Length > 50)
            return (false, "رقم العملية طويل جداً (الحد الأقصى 50 حرف).");

        if (!string.IsNullOrWhiteSpace(senderPhoneNumber) && senderPhoneNumber.Trim().Length > 15)
            return (false, "رقم الهاتف المرسل طويل جداً.");

        var alreadyEnrolled = await _context.Enrollments
            .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        if (alreadyEnrolled)
            return (false, "أنت مشترك بالفعل في هذا الكورس.");

        var hasPending = await _context.PaymentRequests
            .AnyAsync(p => p.StudentId == studentId && p.CourseId == courseId && p.Status == PaymentStatus.Pending);
        if (hasPending)
            return (false, "لديك طلب دفع معلّق لهذا الكورس بالفعل، انتظر مراجعة الإدارة.");

        // FR-32: a transaction number cannot be reused with the same payment method.
        var transactionUsed = await _context.PaymentRequests
            .AnyAsync(p => p.PaymentMethod == method && p.TransactionNumber == trimmedTransaction);
        if (transactionUsed)
            return (false, "رقم العملية مستخدم من قبل مع نفس طريقة الدفع.");

        var storedPath = await _receiptStorage.SaveAsync(receiptContent, receiptFileName);
        if (storedPath == null)
            return (false, "صورة الإيصال غير مقبولة. المسموح: صور JPG أو PNG أو WEBP أو ملف PDF، بحجم أقصى 5 ميجابايت.");

        // FR-12: the course price is stored with the request at submission time,
        // so a later price change does not alter what the student actually paid.
        _context.PaymentRequests.Add(new PaymentRequest
        {
            StudentId = studentId,
            CourseId = courseId,
            Amount = course.Price,
            PaymentMethod = method,
            ReceiptImagePath = storedPath,
            TransactionNumber = trimmedTransaction,
            SenderPhoneNumber = string.IsNullOrWhiteSpace(senderPhoneNumber) ? null : senderPhoneNumber.Trim(),
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The unique index on (PaymentMethod, TransactionNumber) caught a race
            // where the same receipt was submitted twice at the same moment.
            return (false, "رقم العملية مستخدم من قبل مع نفس طريقة الدفع.");
        }

        return (true, "تم إرسال طلب الدفع، وسيتم مراجعته من الإدارة.");
    }

    public async Task<List<MyPaymentRequestViewModel>> GetMyPaymentRequestsAsync(int studentId)
    {
        return await _context.PaymentRequests
            .Where(p => p.StudentId == studentId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new MyPaymentRequestViewModel
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
    }

    public async Task<List<CourseCardViewModel>> GetMyCoursesAsync(int studentId)
    {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new CourseCardViewModel
            {
                Id = e.Course.Id,
                Title = e.Course.Title,
                Description = e.Course.Description,
                Price = e.Course.Price,
                TeacherName = e.Course.Teacher.FullName,
                LessonsCount = e.Course.Lessons.Count,
                IsEnrolled = true
            })
            .ToListAsync();
    }

    public async Task<WatchCourseViewModel?> GetWatchCourseAsync(int courseId, int studentId)
    {
        var enrolled = await _context.Enrollments
            .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        if (!enrolled)
            return null;

        var courseTitle = await _context.Courses
            .Where(c => c.Id == courseId)
            .Select(c => c.Title)
            .FirstAsync();

        var lessons = await _context.Lessons
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.OrderIndex)
            .Select(l => new WatchLessonViewModel
            {
                Id = l.Id,
                Title = l.Title,
                VideoUrl = l.VideoUrl,
                Order = l.OrderIndex,
                IsCompleted = l.Progress.Any(p => p.StudentId == studentId && p.IsCompleted)
            })
            .ToListAsync();

        return new WatchCourseViewModel { CourseId = courseId, CourseTitle = courseTitle, Lessons = lessons };
    }

    public async Task<(bool Success, string Message)> MarkLessonCompletedAsync(int lessonId, int studentId)
    {
        var lesson = await _context.Lessons
            .Where(l => l.Id == lessonId)
            .Select(l => new { l.Id, l.CourseId })
            .FirstOrDefaultAsync();
        if (lesson == null)
            return (false, "الدرس غير موجود.");

        var enrolled = await _context.Enrollments
            .AnyAsync(e => e.StudentId == studentId && e.CourseId == lesson.CourseId);
        if (!enrolled)
            return (false, "يجب الاشتراك في الكورس أولاً.");

        var progress = await _context.LessonProgress
            .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LessonId == lessonId);
        if (progress == null)
        {
            _context.LessonProgress.Add(new LessonProgress
            {
                StudentId = studentId,
                LessonId = lessonId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            });
        }
        else
        {
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return (true, "تم تسجيل إتمام الدرس.");
    }
}
