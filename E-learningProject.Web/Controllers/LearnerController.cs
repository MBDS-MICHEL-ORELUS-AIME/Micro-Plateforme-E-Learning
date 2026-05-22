using E_learningProject.Data.Context;
using E_learningProject.Services.Interfaces;
using E_learningProject.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Web.Controllers;

public class LearnerController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IProgressService _progressService;

    public LearnerController(ApplicationDbContext dbContext, IProgressService progressService)
    {
        _dbContext = dbContext;
        _progressService = progressService;
    }

    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var studentId = ResolveStudentId();
        if (studentId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Dashboard), "Learner") });
        }

        var modules = await _dbContext.Modules
            .AsNoTracking()
            .Include(m => m.Lessons)
            .OrderBy(m => m.Title)
            .ToListAsync(cancellationToken);

        var readLessonIds = await _dbContext.LessonProgressions
            .AsNoTracking()
            .Where(lp => lp.StudentId == studentId && lp.IsRead)
            .Select(lp => lp.LessonId)
            .ToListAsync(cancellationToken);

        var moduleCards = modules.Select(m =>
        {
            var total = m.Lessons.Count;
            var read = m.Lessons.Count(l => readLessonIds.Contains(l.Id));
            return new LearnerModuleCardViewModel
            {
                ModuleId = m.Id,
                Title = m.Title,
                Description = m.Description,
                TotalLessons = total,
                ReadLessons = read,
                ProgressPercent = _progressService.CalculateCompletion(read, total)
            };
        }).ToList();

        var overallTotal = moduleCards.Sum(m => m.TotalLessons);
        var overallRead = moduleCards.Sum(m => m.ReadLessons);
        var completedModules = moduleCards.Count(m => m.ProgressPercent >= 100m);

        var certificatesEarned = await _dbContext.Certificates
            .AsNoTracking()
            .CountAsync(c => c.StudentId == studentId, cancellationToken);

        var quizAttempts = await _dbContext.QuizResults
            .AsNoTracking()
            .CountAsync(r => r.StudentId == studentId, cancellationToken);

        var passedQuizzes = await _dbContext.QuizResults
            .AsNoTracking()
            .CountAsync(r => r.StudentId == studentId && r.IsPassed, cancellationToken);

        var discussionsOpened = await _dbContext.DiscussionThreads
            .AsNoTracking()
            .CountAsync(t => t.StudentId == studentId, cancellationToken);

        var viewModel = new LearnerDashboardViewModel
        {
            StudentId = studentId,
            OverallProgress = _progressService.CalculateCompletion(overallRead, overallTotal),
            CompletedModules = completedModules,
            CertificatesEarned = certificatesEarned,
            QuizAttempts = quizAttempts,
            PassedQuizzes = passedQuizzes,
            DiscussionsOpened = discussionsOpened,
            Modules = moduleCards
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Reader(int moduleId, int? lessonId = null, CancellationToken cancellationToken = default)
    {
        var studentId = ResolveStudentId();
        if (studentId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Reader), "Learner", new { moduleId, lessonId }) });
        }

        var module = await _dbContext.Modules
            .AsNoTracking()
            .Include(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

        if (module is null)
        {
            return NotFound();
        }

        var orderedLessons = module.Lessons.OrderBy(l => l.Order).ThenBy(l => l.Id).ToList();

        var readLessonIds = await _dbContext.LessonProgressions
            .AsNoTracking()
            .Where(lp => lp.StudentId == studentId && lp.IsRead)
            .Join(_dbContext.Lessons, lp => lp.LessonId, lesson => lesson.Id, (lp, lesson) => new { lp, lesson })
            .Where(x => x.lesson.ModuleId == moduleId)
            .Select(x => x.lp.LessonId)
            .ToListAsync(cancellationToken);

        var selectedLesson = lessonId.HasValue
            ? orderedLessons.FirstOrDefault(l => l.Id == lessonId.Value)
            : orderedLessons.FirstOrDefault();

        var viewModel = new LearnerReaderViewModel
        {
            ModuleId = moduleId,
            ModuleTitle = module.Title,
            StudentId = studentId,
            ProgressPercent = _progressService.CalculateCompletion(readLessonIds.Count, orderedLessons.Count),
            Lessons = orderedLessons.Select(l => new LessonReaderItemViewModel
            {
                LessonId = l.Id,
                Order = l.Order,
                Title = l.Title,
                TextContent = l.TextContent,
                VideoUrl = l.VideoUrl,
                PdfPath = l.PdfPath,
                IsRead = readLessonIds.Contains(l.Id)
            }).ToList(),
            CurrentLesson = selectedLesson is null ? null : new LessonReaderItemViewModel
            {
                LessonId = selectedLesson.Id,
                Order = selectedLesson.Order,
                Title = selectedLesson.Title,
                TextContent = selectedLesson.TextContent,
                VideoUrl = selectedLesson.VideoUrl,
                PdfPath = selectedLesson.PdfPath,
                IsRead = readLessonIds.Contains(selectedLesson.Id)
            }
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int moduleId, int lessonId, CancellationToken cancellationToken)
    {
        var studentId = ResolveStudentId();
        if (studentId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Reader), "Learner", new { moduleId, lessonId }) });
        }

        var lesson = await _dbContext.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId && l.ModuleId == moduleId, cancellationToken);
        if (lesson is null)
        {
            return NotFound();
        }

        var progression = await _dbContext.LessonProgressions
            .FirstOrDefaultAsync(lp => lp.StudentId == studentId && lp.LessonId == lessonId, cancellationToken);

        if (progression is null)
        {
            progression = new Core.Entities.LessonProgression
            {
                StudentId = studentId,
                LessonId = lessonId,
                IsRead = true,
                ReadDate = DateTime.Now
            };
            _dbContext.LessonProgressions.Add(progression);
        }
        else
        {
            progression.IsRead = true;
            progression.ReadDate = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Reader), new { moduleId, lessonId });
    }

    private string? ResolveStudentId()
    {
        var currentUserName = HttpContext.Session.GetString("CurrentUserName");
        var currentRole = HttpContext.Session.GetString("CurrentUserRole");

        if (!string.IsNullOrWhiteSpace(currentUserName)
            && string.Equals(currentRole, "etudiant", StringComparison.OrdinalIgnoreCase))
        {
            return currentUserName;
        }

        return null;
    }
}
