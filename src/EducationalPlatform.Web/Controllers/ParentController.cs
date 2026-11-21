using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models.ViewModels.Parent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationalPlatform.Web.Controllers;

/// <summary>Parent monitoring: linked children, their lessons progress, and exam grades.</summary>
[Authorize(Roles = "Parent")]
public class ParentController : Controller
{
    private readonly ApplicationDbContext _context;

    public ParentController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int ParentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var links = await _context.ParentStudents
            .Where(ps => ps.ParentId == ParentId)
            .Select(ps => new ParentChildViewModel
            {
                StudentId = ps.StudentId,
                StudentName = ps.Student.FullName,
                StudentPhone = ps.Student.PhoneNumber ?? "",
                LinkedAt = ps.LinkedAt,
                EnrolledCourseCount = _context.Enrollments.Count(e => e.StudentId == ps.StudentId),
                PendingPaymentCount = _context.PaymentRequests.Count(p =>
                    p.StudentId == ps.StudentId && p.Status == Models.Enums.PaymentStatus.Pending)
            })
            .ToListAsync();

        return View(links);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int studentId)
    {
        var linked = await _context.ParentStudents
            .AnyAsync(ps => ps.ParentId == ParentId && ps.StudentId == studentId);
        if (!linked)
            return Forbid();

        var student = await _context.Users
            .Where(u => u.Id == studentId)
            .Select(u => new { u.FullName, u.PhoneNumber })
            .FirstOrDefaultAsync();
        if (student == null)
            return NotFound();

        var progress = await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .Select(e => new ParentChildCourseProgressViewModel
            {
                CourseTitle = e.Course.Title,
                EnrolledAt = e.EnrolledAt,
                TotalLessons = e.Course.Lessons.Count,
                CompletedLessons = _context.LessonProgress.Count(lp =>
                    lp.StudentId == studentId && lp.IsCompleted && lp.Lesson.CourseId == e.CourseId),
                // Grade visibility rule: only fully-graded attempts (no essays pending)
                // on published exams are shown. A Submitted attempt with ungraded essays
                // carries a null/partial score, so it is reported as pending, never as a grade.
                Grades = _context.ExamAttempts
                    .Where(a => a.StudentId == studentId && a.Exam.CourseId == e.CourseId
                        && a.Exam.IsPublished
                        && a.Status == Models.Enums.AttemptStatus.Graded)
                    .OrderByDescending(a => a.SubmittedAt)
                    .Select(a => new ParentChildExamGradeViewModel
                    {
                        ExamTitle = a.Exam.Title,
                        Score = a.TotalScore,
                        TotalMarks = a.Exam.Questions.Sum(q => q.Marks),
                        SubmittedAt = a.SubmittedAt
                    })
                    .ToList(),
                PendingGradingCount = _context.ExamAttempts.Count(a =>
                    a.StudentId == studentId && a.Exam.CourseId == e.CourseId
                    && a.Exam.IsPublished
                    && a.Status == Models.Enums.AttemptStatus.Submitted)
            })
            .ToListAsync();

        var model = new ParentChildDetailsViewModel
        {
            StudentId = studentId,
            StudentName = student.FullName,
            StudentPhone = student.PhoneNumber ?? "",
            Courses = progress
        };
        return View(model);
    }
}
