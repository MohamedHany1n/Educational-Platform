using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Models.ViewModels.Student;

namespace EducationalPlatform.Web.Services;

public interface IStudentService
{
    /// <summary>FR-10: published courses, optionally filtered by a title search.</summary>
    Task<List<CourseCardViewModel>> GetPublishedCoursesAsync(string? search, int studentId);

    /// <summary>Published course details, or null when missing or unpublished.</summary>
    Task<CourseDetailsViewModel?> GetCourseDetailsAsync(int courseId, int studentId);

    /// <summary>
    /// FR-11/FR-12/FR-32: submit an enrollment payment request. The price is
    /// snapshotted from the course at submission time inside the service, and
    /// a reused transaction number for the same method is refused with a
    /// friendly message.
    /// </summary>
    Task<(bool Success, string Message)> SubmitPaymentRequestAsync(
        int studentId, int courseId, PaymentMethod method,
        string transactionNumber, string? senderPhoneNumber,
        Stream receiptContent, string receiptFileName);

    /// <summary>FR-16: the student's own payment requests, newest first.</summary>
    Task<List<MyPaymentRequestViewModel>> GetMyPaymentRequestsAsync(int studentId);

    /// <summary>Courses the student is enrolled in (approved payments).</summary>
    Task<List<CourseCardViewModel>> GetMyCoursesAsync(int studentId);

    /// <summary>FR-17: lessons of an enrolled course with completion state, or null when not enrolled.</summary>
    Task<WatchCourseViewModel?> GetWatchCourseAsync(int courseId, int studentId);

    /// <summary>FR-18: mark a lesson complete for an enrolled student (manual completion, pending team decision).</summary>
    Task<(bool Success, string Message)> MarkLessonCompletedAsync(int lessonId, int studentId);
}
