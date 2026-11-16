using EducationalPlatform.Web.Data;
using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.Enums;
using EducationalPlatform.Web.Models.ViewModels;
using EducationalPlatform.Web.ViewModels;
using global::EducationalPlatform.Web.Data;
using global::EducationalPlatform.Web.Models;
using global::EducationalPlatform.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace EducationalPlatform.Web.Controllers;

[Authorize(Roles = "Teacher")]
public class ExamsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExamsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Exams/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int teacherId))
        {
            return Forbid();
        }

        await LoadTeacherCourses(teacherId);

        return View();
    }

    // POST: Exams/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExamViewModel model)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int teacherId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await LoadTeacherCourses(teacherId, model.CourseId);
            return View(model);
        }

        // Verify that the selected course belongs to this teacher
        bool courseExists = await _context.Courses.AnyAsync(c =>
            c.Id == model.CourseId &&
            c.TeacherId == teacherId);

        if (!courseExists)
        {
            ModelState.AddModelError(
                nameof(model.CourseId),
                "الكورس المختار غير صحيح.");

            await LoadTeacherCourses(teacherId, model.CourseId);
            return View(model);
        }

        var exam = new Exam
        {
            CourseId = model.CourseId,
            Title = model.Title,
            DurationMinutes = model.DurationMinutes,
            MaxAttempts = model.MaxAttempts,
            IsPublished = false
        };

        _context.Exams.Add(exam);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم إنشاء الامتحان بنجاح.";

        return RedirectToAction(
     nameof(AddQuestion),
     new { examId = exam.Id });
    }

    private async Task LoadTeacherCourses(
        int teacherId,
        int? selectedCourseId = null)
    {
        var courses = await _context.Courses
            .Where(c => c.TeacherId == teacherId)
            .OrderBy(c => c.Title)
            .ToListAsync();

        ViewBag.Courses = new SelectList(
            courses,
            "Id",
            "Title",
            selectedCourseId);
    }


    [HttpGet]
    public async Task<IActionResult> AddQuestion(int examId)
    {
        int teacherId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        bool ownsExam = await _context.Exams.AnyAsync(e =>
            e.Id == examId &&
            e.Course.TeacherId == teacherId);

        if (!ownsExam)
            return NotFound();

        var model = new AddQuestionViewModel
        {
            ExamId = examId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(
        AddQuestionViewModel model)
    {
        int teacherId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        bool ownsExam = await _context.Exams.AnyAsync(e =>
            e.Id == model.ExamId &&
            e.Course.TeacherId == teacherId);

        if (!ownsExam)
            return NotFound();

        if (!Enum.IsDefined(typeof(QuestionType), model.Type))
        {
            ModelState.AddModelError(
                nameof(model.Type), "نوع السؤال غير صحيح.");
        }


        List<string> validOptions = model.Options
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim())
            .ToList();

        if (model.Type == QuestionType.MCQ)
        {
            // At least two options are required
            if (validOptions.Count < 2)
            {
                ModelState.AddModelError(
                    nameof(model.Options),
                    "يجب إضافة اختيارين على الأقل.");
            }

            // Remove duplicate indexes
            model.CorrectOptionIndexes = model.CorrectOptionIndexes
                .Distinct()
                .ToList();

            // At least one correct answer is required
            if (model.CorrectOptionIndexes.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.CorrectOptionIndexes),
                    "يجب تحديد إجابة صحيحة واحدة على الأقل.");
            }

            // Validate selected indexes
            if (model.CorrectOptionIndexes.Any(index =>
                index < 0 ||
                index >= model.Options.Count ||
                string.IsNullOrWhiteSpace(model.Options[index])))
            {
                ModelState.AddModelError(
                    nameof(model.CorrectOptionIndexes),
                    "يوجد اختيار صحيح غير صالح.");
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        var question = new Question
        {
            ExamId = model.ExamId,
            Text = model.Text.Trim(),
            Type = model.Type,
            Marks = model.Marks
        };

        if (model.Type == QuestionType.MCQ)
        {
            for (int i = 0; i < model.Options.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(model.Options[i]))
                    continue;

                question.Options.Add(new QuestionOption
                {
                    Text = model.Options[i].Trim(),
                    IsCorrect = model.CorrectOptionIndexes.Contains(i)
                });
            }
        }

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم إضافة السؤال بنجاح.";

        return RedirectToAction(
            nameof(AddQuestion),
            new { examId = model.ExamId });


    }
    [HttpGet]
    public async Task<IActionResult> MyExams()
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int teacherId))
        {
            return Forbid();
        }

        var exams = await _context.Exams
            .Where(e => e.Course.TeacherId == teacherId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new MyExamViewModel
            {
                Id = e.Id,
                Title = e.Title,
                CourseTitle = e.Course.Title,
                DurationMinutes = e.DurationMinutes,
                MaxAttempts = e.MaxAttempts,
                QuestionsCount = e.Questions.Count,
                IsPublished = e.IsPublished
            })
            .ToListAsync();

        return View(exams);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int teacherId))
        {
            return Forbid();
        }

        var exam = await _context.Exams
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                e.Course.TeacherId == teacherId);

        if (exam == null)
            return NotFound();

        // Prevent publishing an exam without questions
        if (!exam.IsPublished)
        {
            bool hasQuestions = await _context.Questions
                .AnyAsync(q => q.ExamId == id);

            if (!hasQuestions)
            {
                TempData["ErrorMessage"] =
                    "لا يمكن نشر امتحان بدون أسئلة.";

                return RedirectToAction(nameof(MyExams));
            }
        }

        exam.IsPublished = !exam.IsPublished;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = exam.IsPublished
            ? "تم نشر الامتحان بنجاح."
            : "تم إلغاء نشر الامتحان.";

        return RedirectToAction(nameof(MyExams));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditExamViewModel model)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int teacherId))
        {
            return Forbid();
        }

        var exam = await _context.Exams
            .FirstOrDefaultAsync(e =>
                e.Id == model.Id &&
                e.Course.TeacherId == teacherId);

        if (exam == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        exam.Title = model.Title.Trim();
        exam.DurationMinutes = model.DurationMinutes;
        exam.MaxAttempts = model.MaxAttempts;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم تعديل بيانات الامتحان بنجاح.";

        return RedirectToAction(nameof(MyExams));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int teacherId))
        {
            return Forbid();
        }

        var exam = await _context.Exams
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                e.Course.TeacherId == teacherId);

        if (exam == null)
            return NotFound();

        var model = new EditExamViewModel
        {
            Id = exam.Id,
            Title = exam.Title,
            DurationMinutes = exam.DurationMinutes,
            MaxAttempts = exam.MaxAttempts
        };

        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> ManageQuestions(int examId)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int teacherId))
        {
            return Forbid();
        }

        var exam = await _context.Exams
            .Where(e =>
                e.Id == examId &&
                e.Course.TeacherId == teacherId)
            .Select(e => new
            {
                e.Id,
                e.Title
            })
            .FirstOrDefaultAsync();

        if (exam == null)
            return NotFound();

        var questions = await _context.Questions
            .Where(q => q.ExamId == examId)
            .Select(q => new ManageQuestionViewModel
            {
                Id = q.Id,
                Text = q.Text,
                Type = q.Type,
                Marks = q.Marks,
                OptionsCount = q.Options.Count
            })
            .ToListAsync();

        ViewBag.ExamId = exam.Id;
        ViewBag.ExamTitle = exam.Title;

        return View(questions);
    }

    [HttpGet]
    public async Task<IActionResult> EditQuestion(int id)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int teacherId))
        {
            return Forbid();
        }

        var question = await _context.Questions
            .Include(q => q.Options)
            .Include(q => q.Exam)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(q =>
                q.Id == id &&
                q.Exam.Course.TeacherId == teacherId);

        if (question == null)
            return NotFound();

        bool hasAttempts = await _context.ExamAttempts
            .AnyAsync(a => a.ExamId == question.ExamId);

        if (hasAttempts)
        {
            TempData["ErrorMessage"] =
                "لا يمكن تعديل الأسئلة بعد بدء الطلاب للامتحان.";

            return RedirectToAction(
                nameof(ManageQuestions),
                new { examId = question.ExamId });
        }

        var options = question.Options
            .OrderBy(o => o.Id)
            .ToList();

        var model = new EditQuestionViewModel
        {
            Id = question.Id,
            ExamId = question.ExamId,
            Text = question.Text,
            Type = question.Type,
            Marks = question.Marks,
            Options = options.Select(o => o.Text).ToList(),
            CorrectOptionIndexes = options
                .Select((option, index) => new { option, index })
                .Where(x => x.option.IsCorrect)
                .Select(x => x.index)
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuestion(
        EditQuestionViewModel model)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int teacherId))
        {
            return Forbid();
        }

        var question = await _context.Questions
            .Include(q => q.Options)
            .Include(q => q.Exam)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(q =>
                q.Id == model.Id &&
                q.Exam.Course.TeacherId == teacherId);

        if (question == null)
            return NotFound();

        if (question.ExamId != model.ExamId)
            return BadRequest();

        bool hasAttempts = await _context.ExamAttempts
            .AnyAsync(a => a.ExamId == question.ExamId);

        if (hasAttempts)
        {
            TempData["ErrorMessage"] =
                "لا يمكن تعديل الأسئلة بعد بدء الطلاب للامتحان.";

            return RedirectToAction(
                nameof(ManageQuestions),
                new { examId = question.ExamId });
        }

        if (!Enum.IsDefined(typeof(QuestionType), model.Type))
        {
            ModelState.AddModelError(
                nameof(model.Type),
                "نوع السؤال غير صحيح.");
        }

        if (model.Type == QuestionType.MCQ)
        {
            var validOptions = (model.Options ?? new List<string>())
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim())
                .ToList();

            var correctIndexes = (model.CorrectOptionIndexes ?? new List<int>())
                .Distinct()
                .ToList();

            if (validOptions.Count < 2)
            {
                ModelState.AddModelError(
                    nameof(model.Options),
                    "يجب إضافة اختيارين على الأقل.");
            }

            if (correctIndexes.Count == 0 ||
                correctIndexes.Any(i =>
                    i < 0 ||
                    i >= (model.Options?.Count ?? 0) ||
                    string.IsNullOrWhiteSpace(model.Options[i])))
            {
                ModelState.AddModelError(
                    nameof(model.CorrectOptionIndexes),
                    "يجب تحديد إجابة صحيحة واحدة على الأقل.");
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        question.Text = model.Text.Trim();
        question.Type = model.Type;
        question.Marks = model.Marks;

        _context.QuestionOptions.RemoveRange(question.Options);

        if (model.Type == QuestionType.MCQ)
        {
            var validOptions = model.Options
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim())
                .ToList();

            var correctIndexes = model.CorrectOptionIndexes
                .Distinct()
                .ToList();

            for (int i = 0; i < validOptions.Count; i++)
            {
                question.Options.Add(new QuestionOption
                {
                    Text = validOptions[i],
                    IsCorrect = correctIndexes.Contains(i)
                });
            }
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم تعديل السؤال بنجاح.";

        return RedirectToAction(
            nameof(ManageQuestions),
            new { examId = question.ExamId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int teacherId))
        {
            return Forbid();
        }

        var question = await _context.Questions
            .Include(q => q.Exam)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(q =>
                q.Id == id &&
                q.Exam.Course.TeacherId == teacherId);

        if (question == null)
            return NotFound();

        bool hasAttempts = await _context.ExamAttempts
            .AnyAsync(a => a.ExamId == question.ExamId);

        if (hasAttempts)
        {
            TempData["ErrorMessage"] =
                "لا يمكن حذف الأسئلة بعد بدء الطلاب للامتحان.";

            return RedirectToAction(
                nameof(ManageQuestions),
                new { examId = question.ExamId });
        }

        int examId = question.ExamId;

        _context.Questions.Remove(question);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم حذف السؤال بنجاح.";

        return RedirectToAction(
            nameof(ManageQuestions),
            new { examId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExam(int id)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int teacherId))
        {
            return Forbid();
        }

        var exam = await _context.Exams
            .Include(e => e.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                e.Course.TeacherId == teacherId);

        if (exam == null)
            return NotFound();

        bool hasAttempts = await _context.ExamAttempts
            .AnyAsync(a => a.ExamId == id);

        if (hasAttempts)
        {
            TempData["ErrorMessage"] =
                "لا يمكن حذف الامتحان لوجود محاولات طلاب مسجلة عليه.";

            return RedirectToAction(nameof(MyExams));
        }

        _context.Exams.Remove(exam);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم حذف الامتحان وجميع أسئلته بنجاح.";

        return RedirectToAction(nameof(MyExams));
    }

}
