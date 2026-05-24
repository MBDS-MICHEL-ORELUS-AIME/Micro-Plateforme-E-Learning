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
            Modules = moduleCards,
            Badges = await _dbContext.StudentBadges.AsNoTracking().Where(b => b.StudentId == studentId).OrderByDescending(b => b.AwardedAt).Select(b => new BadgeViewModel { Name = b.BadgeName, Description = b.Description, IconCss = b.IconCss, AwardedAt = b.AwardedAt }).Take(4).ToListAsync(cancellationToken)
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

    public async Task<IActionResult> QuizHistory(CancellationToken cancellationToken = default)
    {
        var studentId = ResolveStudentId();
        if (studentId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(QuizHistory), "Learner") });
        }

        var attempts = await _dbContext.QuizResults
            .AsNoTracking()
            .Where(r => r.StudentId == studentId)
            .Include(r => r.Quiz)
            .OrderByDescending(r => r.AttemptDate)
            .Select(r => new LearnerQuizHistoryItemViewModel
            {
                AttemptId = r.Id,
                QuizTitle = r.Quiz != null ? r.Quiz.Title : "Quiz",
                Score = r.Score,
                IsPassed = r.IsPassed,
                AttemptDate = r.AttemptDate
            })
            .ToListAsync(cancellationToken);

        var viewModel = new LearnerQuizHistoryViewModel
        {
            StudentId = studentId,
            TotalAttempts = attempts.Count,
            PassedAttempts = attempts.Count(a => a.IsPassed),
            AverageScore = attempts.Count == 0 ? 0 : Math.Round(attempts.Average(a => a.Score), 1),
            Attempts = attempts
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
        await AwardBadgesAfterReadAsync(studentId, moduleId, cancellationToken);
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
    public async Task<IActionResult> Badges(CancellationToken cancellationToken = default)
    {
        var studentId = ResolveStudentId();
        if (studentId is null)
            return RedirectToAction("Login", "Account");

        var badges = await _dbContext.StudentBadges
            .AsNoTracking()
            .Where(b => b.StudentId == studentId)
            .OrderByDescending(b => b.AwardedAt)
            .Select(b => new BadgeViewModel
            {
                Name = b.BadgeName,
                Description = b.Description,
                IconCss = b.IconCss,
                AwardedAt = b.AwardedAt
            })
            .ToListAsync(cancellationToken);

        return View(new LearnerBadgesViewModel { StudentId = studentId, Badges = badges });
    }

    private async Task AwardBadgesAfterReadAsync(string studentId, int moduleId, CancellationToken cancellationToken)
    {
        var module = await _dbContext.Modules.AsNoTracking().Include(m => m.Lessons).FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);
        if (module is null) return;

        var readLessonIds = await _dbContext.LessonProgressions.AsNoTracking()
            .Where(lp => lp.StudentId == studentId && lp.IsRead)
            .Select(lp => lp.LessonId)
            .ToListAsync(cancellationToken);

        if (readLessonIds.Count >= 1)
            await TryAwardBadgeAsync(studentId, "Premiere Lecon", "Vous avez lu votre premiere lecon !", "bi-star-fill", cancellationToken);

        if (module.Lessons.Count > 0 && module.Lessons.All(l => readLessonIds.Contains(l.Id)))
            await TryAwardBadgeAsync(studentId, "Module: " + module.Title, "Module completement lu: " + module.Title, "bi-trophy-fill", cancellationToken);
    }

    private async Task TryAwardBadgeAsync(string studentId, string badgeName, string description, string iconCss, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.StudentBadges.AnyAsync(b => b.StudentId == studentId && b.BadgeName == badgeName, cancellationToken);
        if (!exists)
        {
            _dbContext.StudentBadges.Add(new Core.Entities.StudentBadge
            {
                StudentId = studentId,
                BadgeName = badgeName,
                Description = description,
                IconCss = iconCss,
                AwardedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}