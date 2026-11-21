using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

    // SaveAnswer (and the answers posted with Submit) are still accepted for a
    // few seconds after expiry so the browser's final flush at zero is not lost
    // to network latency. Set to 0 for a strict cutoff.
    private const int SaveGraceSeconds = 5;

    // A manual Submit that arrives up to this many seconds BEFORE the expiry is
    // treated as "time is up" (no completeness check). It absorbs small client
    // clock / latency differences so an automatic submission is never rejected.
    private const int SubmitToleranceSeconds = 3;

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

        // Previously this used IsolationLevel.Serializable, which makes two
        // simultaneous Start requests (double click) take conflicting range
        // locks and deadlock. A per-(exam, student) application lock gives the
        // same guarantee - one Start at a time - without that risk.
        return await RunWithLockAsync($"ExamStart:{id}:{studentId}", async () =>
        {
            var exam = await _context.Exams
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.Id == id &&
                    e.IsPublished &&
                    e.Course.Enrollments.Any(en =>
                        en.StudentId == studentId));

            if (exam == null)
                return NotFound();

            // Check for an existing active attempt.
            var activeAttempt = await _context.ExamAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.ExamId == id &&
                    a.StudentId == studentId &&
                    a.Status == AttemptStatus.InProgress);

            if (activeAttempt != null)
            {
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

            return RedirectToAction("Take", new { id = attempt.Id });
        });
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

        // 1) Cheap header query first: ownership, status and expiry are decided
        //    before any question data is loaded.
        var header = await _context.ExamAttempts
            .AsNoTracking()
            .Where(a =>
                a.Id == id &&
                a.StudentId == studentId &&
                a.Status == AttemptStatus.InProgress)
            .Select(a => new
            {
                AttemptId = a.Id,
                a.ExamId,
                ExamTitle = a.Exam.Title,
                a.StartedAt,
                a.Exam.DurationMinutes
            })
            .FirstOrDefaultAsync();

        if (header == null)
            return NotFound();

        var expiresAt = header.StartedAt.AddMinutes(header.DurationMinutes);

        if (DateTime.UtcNow >= expiresAt)
        {
            return RedirectToAction(nameof(SubmitExpired), new { id });
        }

        // 2) Questions and options.
        var questions = await _context.Questions
            .AsNoTracking()
            .Where(q => q.ExamId == header.ExamId)
            .OrderBy(q => q.Id)
            .Select(q => new TakeQuestionViewModel
            {
                Id = q.Id,
                Text = q.Text,
                Type = q.Type.ToString(),
                Marks = q.Marks,

                Options = q.Options
                    .OrderBy(o => o.Id)
                    .Select(o => new TakeOptionViewModel
                    {
                        Id = o.Id,
                        Text = o.Text
                    })
                    .ToList()
            })
            .ToListAsync();

        // 3) Answers already saved for this attempt, so a page refresh shows
        //    them again (and a later Submit does not overwrite them with blanks).
        var savedAnswers = (await _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.Id == id)
            .SelectMany(a => a.Answers)
            .Select(a => new
            {
                a.QuestionId,
                a.EssayText,
                OptionIds = a.SelectedOptions
                    .Select(o => o.QuestionOptionId)
                    .ToList()
            })
            .ToListAsync())
            .ToDictionary(a => a.QuestionId);

        foreach (var question in questions)
        {
            if (!savedAnswers.TryGetValue(question.Id, out var saved))
                continue;

            question.EssayText = saved.EssayText;

            var selected = saved.OptionIds.ToHashSet();

            foreach (var option in question.Options)
                option.IsSelected = selected.Contains(option.Id);
        }

        var model = new TakeExamViewModel
        {
            AttemptId = header.AttemptId,
            ExamTitle = header.ExamTitle,
            StartedAt = header.StartedAt,
            DurationMinutes = header.DurationMinutes,
            Questions = questions
        };

        return View(model);
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

        return await RunWithLockAsync(AttemptLockResource(id), async () =>
        {
            var attempt = await LoadAttemptForSubmissionAsync(id, studentId);

            if (attempt == null)
                return NotFound();

            if (attempt.Status != AttemptStatus.InProgress)
                return RedirectToAction(nameof(Available));

            var expiresAt = attempt.StartedAt.AddMinutes(
                attempt.Exam.DurationMinutes);

            if (DateTime.UtcNow < expiresAt)
                return RedirectToAction(nameof(Take), new { id });

            var questionsById = attempt.Exam.Questions
                .ToDictionary(q => q.Id);

            GradeAndFinalize(attempt, questionsById, expiresAt);

            await _context.SaveChangesAsync();

            TempData["ErrorMessage"] = "انتهى وقت الامتحان وتم تسليم إجاباتك المحفوظة.";

            return RedirectToAction(nameof(Available));
        });
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

    [HttpGet]
    public async Task<IActionResult> AttemptDetails(int id)
    {
        var studentIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(studentIdValue, out int studentId))
            return Unauthorized();

        var attempt = await _context.ExamAttempts
            .Where(a => a.Id == id && a.StudentId == studentId)
            .Select(a => new AttemptDetailsViewModel
            {
                AttemptId = a.Id,
                ExamTitle = a.Exam.Title,
                CourseTitle = a.Exam.Course.Title,
                AttemptNumber = a.AttemptNumber,
                Status = a.Status.ToString(),
                TotalScore = a.TotalScore,
                StartedAt = a.StartedAt,
                SubmittedAt = a.SubmittedAt,

                Answers = a.Answers.Select(answer =>
                    new AttemptAnswerDetailsViewModel
                    {
                        QuestionText = answer.Question.Text,
                        QuestionType = answer.Question.Type.ToString(),
                        Marks = answer.Question.Marks,
                        Score = answer.Score,
                        EssayText = answer.EssayText,
                        TeacherFeedback = answer.TeacherFeedback,

                        Options = answer.Question.Options.Select(option =>
                            new AttemptOptionDetailsViewModel
                            {
                                Text = option.Text,
                                IsSelected = answer.SelectedOptions.Any(
                                    selected => selected.QuestionOptionId == option.Id)
                            }).ToList()
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (attempt == null)
            return NotFound();

        return View(attempt);
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

        // Answers posted with the form (null when none were posted).
        var postedAnswers = model?.Answers?.ToList();

        return await RunWithLockAsync(AttemptLockResource(id), async () =>
        {
            var attempt = await LoadAttemptForSubmissionAsync(id, studentId);

            if (attempt == null)
                return NotFound();

            if (attempt.Status != AttemptStatus.InProgress)
                return BadRequest("This attempt has already been submitted.");

            var now = DateTime.UtcNow;

            var expiresAt = attempt.StartedAt.AddMinutes(
                attempt.Exam.DurationMinutes);

            // Time is up (automatic submission): never blocked by missing answers.
            bool isTimeUp = now >= expiresAt.AddSeconds(-SubmitToleranceSeconds);

            // Posted answers are a fallback for the very last edits; they are
            // ignored once the grace window after expiry has passed.
            bool acceptPosted = now < expiresAt.AddSeconds(SaveGraceSeconds);

            var questionsById = attempt.Exam.Questions
                .ToDictionary(q => q.Id);

            var answersByQuestionId = attempt.Answers
                .ToDictionary(a => a.QuestionId);

            if (acceptPosted && postedAnswers != null)
            {
                var submittedQuestionIds = postedAnswers
                    .Select(a => a.QuestionId)
                    .ToList();

                if (submittedQuestionIds.Count != submittedQuestionIds.Distinct().Count() ||
                    submittedQuestionIds.Any(qid => !answersByQuestionId.ContainsKey(qid)))
                {
                    return BadRequest("Invalid submitted answers.");
                }

                foreach (var submitted in postedAnswers)
                {
                    var answer = answersByQuestionId[submitted.QuestionId];

                    if (!questionsById.TryGetValue(answer.QuestionId, out var question))
                        continue;

                    if (question.Type == QuestionType.MCQ)
                    {
                        var selectedIds = (submitted.SelectedOptionIds
                            ?? new List<int>())
                            .Distinct()
                            .ToList();

                        bool validOptions = selectedIds.All(optionId =>
                            question.Options.Any(o => o.Id == optionId));

                        if (!validOptions)
                            return BadRequest("Invalid question options.");

                        ApplySelectedOptions(answer, selectedIds);
                    }
                    else if (question.Type == QuestionType.Essay)
                    {
                        answer.EssayText = submitted.EssayText?.Trim();
                    }
                }
            }

            // Manual submission: reject incomplete attempts on the server.
            // Nothing has been saved at this point, so the stored answers stay as they were.
            if (!isTimeUp)
            {
                bool hasUnanswered = attempt.Answers.Any(a =>
                    questionsById.TryGetValue(a.QuestionId, out var q) &&
                    !IsAnswered(a, q));

                if (hasUnanswered)
                {
                    TempData["SubmitError"] =
                        "لا يمكن تسليم الامتحان قبل الإجابة عن جميع الأسئلة.";

                    return RedirectToAction(nameof(Take), new { id });
                }
            }

            GradeAndFinalize(
                attempt,
                questionsById,
                now >= expiresAt ? expiresAt : now);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Available));
        });
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

        return await RunWithLockAsync(AttemptLockResource(id), async () =>
        {
            // Light, untracked read. Status and expiry are checked AFTER the
            // lock was taken, so they reflect a Submit that just committed.
            var attempt = await _context.ExamAttempts
                .AsNoTracking()
                .Where(a => a.Id == id && a.StudentId == studentId)
                .Select(a => new
                {
                    a.Status,
                    a.StartedAt,
                    a.ExamId,
                    a.Exam.DurationMinutes
                })
                .FirstOrDefaultAsync();

            if (attempt == null)
                return NotFound();

            if (attempt.Status != AttemptStatus.InProgress)
                return BadRequest();

            var expiresAt = attempt.StartedAt.AddMinutes(attempt.DurationMinutes);

            if (DateTime.UtcNow >= expiresAt.AddSeconds(SaveGraceSeconds))
                return BadRequest("Exam time has expired.");

            var question = await _context.Questions
                .AsNoTracking()
                .Where(q => q.Id == model.QuestionId && q.ExamId == attempt.ExamId)
                .Select(q => new
                {
                    q.Type,
                    OptionIds = q.Options.Select(o => o.Id).ToList()
                })
                .FirstOrDefaultAsync();

            if (question == null)
                return BadRequest("Invalid question.");

            // Load ONLY the answer being edited, not every answer of the attempt.
            var answer = await _context.ExamAttempts
                .Where(a => a.Id == id)
                .SelectMany(a => a.Answers)
                .Where(a => a.QuestionId == model.QuestionId)
                .Include(a => a.SelectedOptions)
                .FirstOrDefaultAsync();

            if (answer == null)
                return BadRequest("Invalid question.");

            if (question.Type == QuestionType.MCQ)
            {
                var selectedIds = (model.SelectedOptionIds ?? new List<int>())
                    .Distinct()
                    .ToList();

                if (!selectedIds.All(optionId => question.OptionIds.Contains(optionId)))
                    return BadRequest("Invalid options.");

                ApplySelectedOptions(answer, selectedIds);
            }
            else if (question.Type == QuestionType.Essay)
            {
                answer.EssayText = model.EssayText?.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok();
        });
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string AttemptLockResource(int attemptId) =>
        $"ExamAttempt:{attemptId}";

    // Runs `work` inside a transaction that first takes a SQL Server
    // application lock. Every action that writes to an attempt's answers
    // (SaveAnswer, Submit, SubmitExpired) uses the SAME resource name, so they
    // run one at a time per attempt instead of locking the same rows in
    // different orders (the cause of the deadlock). The lock is released
    // automatically when the transaction ends.
    //
    // `work` calls SaveChangesAsync itself when it wants to persist. When it
    // returns without saving (a rejection), the commit is a no-op.
    //
    // The execution strategy wrapper makes this work with or without
    // EnableRetryOnFailure. Every unit of work here is safe to re-run.
    private async Task<IActionResult> RunWithLockAsync(
        string lockResource,
        Func<Task<IActionResult>> work)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            await AcquireLockAsync(lockResource);

            var result = await work();

            await transaction.CommitAsync();

            return result;
        });
    }

    private async Task AcquireLockAsync(string resourceName)
    {
        var resource = new SqlParameter("@Resource", SqlDbType.NVarChar, 255)
        {
            Value = resourceName
        };

        var result = new SqlParameter("@Result", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC @Result = sp_getapplock " +
            "@Resource = @Resource, @LockMode = 'Exclusive', " +
            "@LockOwner = 'Transaction', @LockTimeout = 15000;",
            resource, result);

        // 0 / 1 = granted; negative = timeout, deadlock victim or error.
        if ((int)result.Value < 0)
            throw new InvalidOperationException(
                $"Could not acquire lock '{resourceName}' (code {result.Value}).");
    }

    // Split query: loading Questions->Options and Answers->SelectedOptions in
    // one query multiplies the rows of both collections together.
    private Task<ExamAttempt?> LoadAttemptForSubmissionAsync(int id, int studentId)
    {
        return _context.ExamAttempts
            .Include(a => a.Exam)
                .ThenInclude(e => e.Questions)
                    .ThenInclude(q => q.Options)
            .Include(a => a.Answers)
                .ThenInclude(a => a.SelectedOptions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.StudentId == studentId);
    }

    // Applies only the difference between the stored and the requested
    // options instead of deleting everything and inserting it again.
    private static void ApplySelectedOptions(AttemptAnswer answer, List<int> selectedIds)
    {
        var toRemove = answer.SelectedOptions
            .Where(o => !selectedIds.Contains(o.QuestionOptionId))
            .ToList();

        foreach (var option in toRemove)
            answer.SelectedOptions.Remove(option);

        var existingIds = answer.SelectedOptions
            .Select(o => o.QuestionOptionId)
            .ToHashSet();

        foreach (var optionId in selectedIds.Where(x => !existingIds.Contains(x)))
        {
            answer.SelectedOptions.Add(new AttemptAnswerOption
            {
                QuestionOptionId = optionId
            });
        }
    }

    private static bool IsAnswered(AttemptAnswer answer, Question question)
    {
        if (question.Type == QuestionType.MCQ)
            return answer.SelectedOptions.Count > 0;

        if (question.Type == QuestionType.Essay)
            return !string.IsNullOrWhiteSpace(answer.EssayText);

        return true;
    }

    // Single place where an attempt is graded and closed, used by Submit and
    // SubmitExpired so both follow the same rules:
    //  - MCQ: full marks only if the selected ids equal ALL correct ids;
    //         no selection always scores 0.
    //  - Essay with text: Score stays null (pending teacher grading).
    //  - Essay without text: Score = 0.
    private static void GradeAndFinalize(
        ExamAttempt attempt,
        Dictionary<int, Question> questionsById,
        DateTime submittedAt)
    {
        bool hasUngradedEssays = false;

        foreach (var answer in attempt.Answers)
        {
            if (!questionsById.TryGetValue(answer.QuestionId, out var question))
                continue;

            if (question.Type == QuestionType.MCQ)
            {
                var selectedIds = answer.SelectedOptions
                    .Select(o => o.QuestionOptionId)
                    .ToHashSet();

                var correctIds = question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => o.Id)
                    .ToHashSet();

                bool isCorrect =
                    selectedIds.Count > 0 &&
                    selectedIds.SetEquals(correctIds);

                answer.Score = isCorrect
                    ? question.Marks
                    : 0;
            }
            else if (question.Type == QuestionType.Essay)
            {
                if (string.IsNullOrWhiteSpace(answer.EssayText))
                {
                    answer.Score = 0;
                }
                else
                {
                    answer.Score = null;
                    hasUngradedEssays = true;
                }
            }
        }

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

        attempt.SubmittedAt = submittedAt;
    }
}