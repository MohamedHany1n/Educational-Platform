using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Data;
using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;

namespace EducationalPlatform.Web.Controllers;

[Authorize(Roles = "Student")]
public class StudentExamsController : Controller
{
    private readonly ApplicationDbContext _context;

    public StudentExamsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Available()
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int studentId))
        {
            return Forbid();
        }

        var exams = await _context.Exams
            .Where(e =>
                e.IsPublished &&
                e.Course.Enrollments.Any(en =>
                    en.StudentId == studentId))
            .Select(e => new AvailableExamViewModel
            {
                Id = e.Id,
                Title = e.Title,
                CourseTitle = e.Course.Title,
                DurationMinutes = e.DurationMinutes,
                MaxAttempts = e.MaxAttempts,
                AttemptsUsed = e.Attempts.Count(a =>
                    a.StudentId == studentId)
            })
            .ToListAsync();

        return View(exams);
    }


    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int studentId))
        {
            return Forbid();
        }

        var exam = await _context.Exams
            .Where(e =>
                e.Id == id &&
                e.IsPublished &&
                e.Course.Enrollments.Any(en =>
                    en.StudentId == studentId))
            .Select(e => new ExamDetailsViewModel
            {
                Id = e.Id,
                Title = e.Title,
                CourseTitle = e.Course.Title,
                DurationMinutes = e.DurationMinutes,
                QuestionsCount = e.Questions.Count,
                TotalMarks = e.Questions.Sum(q => q.Marks),
                MaxAttempts = e.MaxAttempts,
                AttemptsUsed = e.Attempts.Count(a =>
                    a.StudentId == studentId)
            })
            .FirstOrDefaultAsync();

        if (exam == null)
            return NotFound();

        return View(exam);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int studentId))
        {
            return Forbid();
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var exam = await _context.Exams
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                e.IsPublished &&
                e.Course.Enrollments.Any(en =>
                    en.StudentId == studentId));

        if (exam == null)
            return NotFound();

        // Check for an existing active attempt.
        var activeAttempt = await _context.ExamAttempts
            .FirstOrDefaultAsync(a =>
                a.ExamId == id &&
                a.StudentId == studentId &&
                a.Status == AttemptStatus.InProgress);

        if (activeAttempt != null)
        {
            await transaction.RollbackAsync();

            var expiresAt = activeAttempt.StartedAt.AddMinutes(
                exam.DurationMinutes);

            if (DateTime.UtcNow >= expiresAt)
            {
                return RedirectToAction(
                    nameof(SubmitExpired),
                    new { id = activeAttempt.Id });
            }

            return RedirectToAction(
                nameof(Take),
                new { id = activeAttempt.Id });
        }

        int attemptsUsed = await _context.ExamAttempts
            .CountAsync(a =>
                a.ExamId == id &&
                a.StudentId == studentId);
        int lastAttemptNumber = await _context.ExamAttempts
        .Where(a =>
        a.ExamId == id &&
        a.StudentId == studentId)
        .Select(a => (int?)a.AttemptNumber)
        .MaxAsync() ?? 0;

        if (attemptsUsed >= exam.MaxAttempts)
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "لقد استنفدت جميع محاولات هذا الامتحان.";

            return RedirectToAction(nameof(Details), new { id });
        }

        var questionIds = await _context.Questions
            .Where(q => q.ExamId == id)
            .Select(q => q.Id)
            .ToListAsync();

        if (questionIds.Count == 0)
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "لا يمكن بدء امتحان بدون أسئلة.";

            return RedirectToAction(nameof(Details), new { id });
        }

        var attempt = new ExamAttempt
        {
            ExamId = id,
            StudentId = studentId,
            AttemptNumber = lastAttemptNumber + 1,
            Status = AttemptStatus.InProgress,
            StartedAt = DateTime.UtcNow,

            Answers = questionIds.Select(questionId =>
                new AttemptAnswer
                {
                    QuestionId = questionId
                }).ToList()
        };

        _context.ExamAttempts.Add(attempt);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToAction("Take", new { id = attempt.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Take(int id)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int studentId))
        {
            return Forbid();
        }

        var attempt = await _context.ExamAttempts
            .Where(a =>
                a.Id == id &&
                a.StudentId == studentId &&
                a.Status == AttemptStatus.InProgress)
            .Select(a => new TakeExamViewModel
            {
                AttemptId = a.Id,
                ExamTitle = a.Exam.Title,
                StartedAt = a.StartedAt,
                DurationMinutes = a.Exam.DurationMinutes,

                Questions = a.Exam.Questions
                    .OrderBy(q => q.Id)
                    .Select(q => new TakeQuestionViewModel
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Type = q.Type.ToString(),
                        Marks = q.Marks,

                        Options = q.Options
                            .Select(o => new TakeOptionViewModel
                            {
                                Id = o.Id,
                                Text = o.Text
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (attempt == null)
            return NotFound();

        var expiresAt = attempt.StartedAt.AddMinutes(attempt.DurationMinutes);

        if (DateTime.UtcNow >= expiresAt)
        {
            return RedirectToAction(nameof(SubmitExpired), new { id });
        }

        return View(attempt);
    }

    [HttpGet]
    public async Task<IActionResult> SubmitExpired(int id)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int studentId))
        {
            return Forbid();
        }

        var attempt = await _context.ExamAttempts
            .Include(a => a.Exam)
                .ThenInclude(e => e.Questions)
                    .ThenInclude(q => q.Options)
            .Include(a => a.Answers)
                .ThenInclude(a => a.SelectedOptions)
            .Include(a => a.Answers)
                .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.StudentId == studentId);

        if (attempt == null)
            return NotFound();

        if (attempt.Status != AttemptStatus.InProgress)
            return RedirectToAction(nameof(Available));

        var expiresAt = attempt.StartedAt.AddMinutes(
            attempt.Exam.DurationMinutes);

        if (DateTime.UtcNow < expiresAt)
            return RedirectToAction(nameof(Take), new { id });

        // Grade MCQ answers
        foreach (var answer in attempt.Answers)
        {
            if (answer.Question.Type != QuestionType.MCQ)
                continue;

            var selectedIds = answer.SelectedOptions
                .Select(o => o.QuestionOptionId)
                .ToHashSet();

            var correctIds = answer.Question.Options
                .Where(o => o.IsCorrect)
                .Select(o => o.Id)
                .ToHashSet();

            bool isCorrect = selectedIds.SetEquals(correctIds);

            answer.Score = isCorrect
                ? answer.Question.Marks
                : 0;
        }

        bool hasUngradedEssays = attempt.Answers.Any(a =>
            a.Question.Type == QuestionType.Essay &&
            a.Score == null);

        attempt.TotalScore = hasUngradedEssays
            ? null
            : attempt.Answers.Sum(a => a.Score ?? 0);

        attempt.Status = hasUngradedEssays
            ? AttemptStatus.Submitted
            : AttemptStatus.Graded;

        attempt.SubmittedAt = expiresAt;

        await _context.SaveChangesAsync();

        TempData["ErrorMessage"] = "انتهى وقت الامتحان وتم تسليم إجاباتك المحفوظة.";

        return RedirectToAction(nameof(Available));
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var studentIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(studentIdValue, out int studentId))
            return Unauthorized();

        var attempts = await _context.ExamAttempts
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.StartedAt)
            .Select(a => new StudentAttemptHistoryViewModel
            {
                AttemptId = a.Id,
                ExamTitle = a.Exam.Title,
                CourseTitle = a.Exam.Course.Title,
                AttemptNumber = a.AttemptNumber,
                StartedAt = a.StartedAt,
                SubmittedAt = a.SubmittedAt,
                Status = a.Status.ToString(),
                TotalScore = a.TotalScore
            })
            .ToListAsync();

        return View(attempts);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        int id,
        SubmitExamViewModel model)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int studentId))
        {
            return Forbid();
        }

        var attempt = await _context.ExamAttempts
            .Include(a => a.Exam)
                .ThenInclude(e => e.Questions)
                    .ThenInclude(q => q.Options)
            .Include(a => a.Answers)
                .ThenInclude(a => a.SelectedOptions)
            .Include(a => a.Answers)
                .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.StudentId == studentId);

        if (attempt == null)
            return NotFound();

        if (attempt.Status != AttemptStatus.InProgress)
            return BadRequest("This attempt has already been submitted.");

        var expiresAt = attempt.StartedAt.AddMinutes(
         attempt.Exam.DurationMinutes);

        bool isAutoSubmitted = DateTime.UtcNow >= expiresAt;

        if (!isAutoSubmitted)
        {
            var expectedQuestionIds = attempt.Answers
                .Select(a => a.QuestionId)
                .ToHashSet();

            var submittedQuestionIds = model.Answers
                .Select(a => a.QuestionId)
                .ToList();

            if (submittedQuestionIds.Count != submittedQuestionIds.Distinct().Count() ||
                submittedQuestionIds.Any(id => !expectedQuestionIds.Contains(id)))
            {
                return BadRequest("Invalid submitted answers.");
            }
        }




        foreach (var answer in attempt.Answers)
        {
            var question = attempt.Exam.Questions
                .First(q => q.Id == answer.QuestionId);

            // Accept submitted answers only if the exam has not expired.
            if (!isAutoSubmitted)
            {
                var submittedAnswer = model.Answers
                    .FirstOrDefault(a => a.QuestionId == answer.QuestionId);

                if (submittedAnswer != null)
                {
                    if (question.Type == QuestionType.MCQ)
                    {
                        var selectedIds = (submittedAnswer.SelectedOptionIds
                            ?? new List<int>())
                            .Distinct()
                            .ToList();

                        bool validOptions = selectedIds.All(optionId =>
                            question.Options.Any(o => o.Id == optionId));

                        if (!validOptions)
                            return BadRequest("Invalid question options.");

                        answer.SelectedOptions = selectedIds
                            .Select(optionId => new AttemptAnswerOption
                            {
                                QuestionOptionId = optionId
                            })
                            .ToList();
                    }
                    else if (question.Type == QuestionType.Essay)
                    {
                        answer.EssayText = submittedAnswer.EssayText?.Trim();
                    }
                }
            }

            // Grade MCQ using the answers stored in the database.
            if (question.Type == QuestionType.MCQ)
            {
                var selectedIds = answer.SelectedOptions
                    .Select(o => o.QuestionOptionId)
                    .ToHashSet();

                var correctIds = question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => o.Id)
                    .ToHashSet();

                answer.Score = selectedIds.SetEquals(correctIds)
                    ? question.Marks
                    : 0;
            }
            else if (question.Type == QuestionType.Essay)
            {
                answer.Score = null;
            }
        }


        bool hasUngradedEssays = attempt.Answers.Any(a =>
            a.Question.Type == QuestionType.Essay &&
            a.Score == null);

        if (hasUngradedEssays)
        {
            attempt.TotalScore = null;
            attempt.Status = AttemptStatus.Submitted;
        }
        else
        {
            attempt.TotalScore = attempt.Answers.Sum(a => a.Score ?? 0);
            attempt.Status = AttemptStatus.Graded;
        }

        attempt.SubmittedAt = isAutoSubmitted? expiresAt : DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Available));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAnswer(
    int id,
    SubmitAnswerViewModel model)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int studentId))
        {
            return Forbid();
        }

        var attempt = await _context.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.Answers)
                .ThenInclude(a => a.SelectedOptions)
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.StudentId == studentId);

        if (attempt == null)
            return NotFound();

        if (attempt.Status != AttemptStatus.InProgress)
            return BadRequest();

        var expiresAt = attempt.StartedAt.AddMinutes(
            attempt.Exam.DurationMinutes);

        if (DateTime.UtcNow >= expiresAt)
            return BadRequest("Exam time has expired.");

        var answer = attempt.Answers
            .FirstOrDefault(a => a.QuestionId == model.QuestionId);

        if (answer == null)
            return BadRequest("Invalid question.");

        var question = await _context.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q =>
                q.Id == model.QuestionId &&
                q.ExamId == attempt.ExamId);

        if (question == null)
            return BadRequest("Invalid question.");

        if (question.Type == QuestionType.MCQ)
        {
            var selectedIds = (model.SelectedOptionIds ?? new List<int>())
                .Distinct()
                .ToList();

            if (!selectedIds.All(id =>
                question.Options.Any(o => o.Id == id)))
            {
                return BadRequest("Invalid options.");
            }

            answer.SelectedOptions.Clear();

            foreach (var optionId in selectedIds)
            {
                answer.SelectedOptions.Add(new AttemptAnswerOption
                {
                    QuestionOptionId = optionId
                });
            }
        }
        else if (question.Type == QuestionType.Essay)
        {
            answer.EssayText = model.EssayText?.Trim();
        }

        await _context.SaveChangesAsync();

        return Ok();
    }
}