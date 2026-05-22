using E_learningProject.Data.Context;
using E_learningProject.Services.Interfaces;
using E_learningProject.Web.Models;
using Microsoft.EntityFrameworkCore;
using E_learningProject.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace E_learningProject.Web.Controllers;

public class CoursesController : Controller
{
    private readonly IModuleRepository _moduleRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly IProgressService _progressService;
    private readonly ICertificateService _certificateService;

    public CoursesController(
        IModuleRepository moduleRepository,
        ApplicationDbContext dbContext,
        IProgressService progressService,
        ICertificateService certificateService)
    {
        _moduleRepository = moduleRepository;
        _dbContext = dbContext;
        _progressService = progressService;
        _certificateService = certificateService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var modules = await _moduleRepository.GetAllAsync(cancellationToken);
        var totalLessons = modules.Sum(m => m.Lessons.Count);
        var totalQuizzes = modules.Count(m => m.QuizId.HasValue);
        var totalEnrollments = await _dbContext.Enrollments.AsNoTracking().CountAsync(cancellationToken);
        var completedEnrollments = await _dbContext.Enrollments.AsNoTracking().CountAsync(e => e.IsCompleted, cancellationToken);

        var viewModel = new CourseCatalogViewModel
        {
            TotalModules = modules.Count,
            TotalLessons = totalLessons,
            TotalQuizzes = totalQuizzes,
            TotalEnrollments = totalEnrollments,
            CompletedEnrollments = completedEnrollments,
            Modules = modules
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var studentId = ResolveStudentId();
        if (studentId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Details), "Courses", new { id }) });
        }

        var module = await _dbContext.Modules
            .AsNoTracking()
            .Include(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (module is null)
        {
            return NotFound();
        }

        var readLessonIds = await _dbContext.LessonProgressions
            .AsNoTracking()
            .Where(lp => lp.StudentId == studentId && lp.IsRead)
            .Join(_dbContext.Lessons, lp => lp.LessonId, lesson => lesson.Id, (lp, lesson) => new { lp, lesson })
            .Where(x => x.lesson.ModuleId == id)
            .Select(x => x.lp.LessonId)
            .ToListAsync(cancellationToken);

        var totalLessons = module.Lessons.Count;
        var completedLessons = module.Lessons.Count(l => readLessonIds.Contains(l.Id));
        var completion = _progressService.CalculateCompletion(completedLessons, totalLessons);

        var viewModel = new CourseProgressViewModel
        {
            ModuleId = module.Id,
            ModuleTitle = module.Title,
            ModuleDescription = module.Description,
            StudentId = studentId,
            CompletionPercentage = completion,
            IsModuleCompleted = completion >= 100,
            Lessons = module.Lessons
                .OrderBy(l => l.Order)
                .Select(l => new LessonProgressItemViewModel
                {
                    LessonId = l.Id,
                    Title = l.Title,
                    TextContent = l.TextContent,
                    VideoUrl = l.VideoUrl,
                    PdfPath = l.PdfPath,
                    Order = l.Order,
                    IsRead = readLessonIds.Contains(l.Id)
                })
                .ToList()
        };

        return View(viewModel);
    }

    public IActionResult Start(int moduleId)
    {
        var role = HttpContext.Session.GetString("CurrentUserRole");

        if (string.IsNullOrWhiteSpace(role))
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Start), "Courses", new { moduleId }) });
        }

        if (string.Equals(role, "etudiant", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Reader", "Learner", new { moduleId });
        }

        if (string.Equals(role, "enseignant", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Workspace", "Teacher");
        }

        if (string.Equals(role, "coordinateur", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkLessonRead(int moduleId, int lessonId, CancellationToken cancellationToken = default)
    {
        var studentId = ResolveStudentId();
        if (studentId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Details), "Courses", new { id = moduleId }) });
        }

        var lesson = await _dbContext.Lessons
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.ModuleId == moduleId, cancellationToken);

        if (lesson is null)
        {
            return NotFound();
        }

        var progression = await _dbContext.LessonProgressions
            .FirstOrDefaultAsync(lp => lp.StudentId == studentId && lp.LessonId == lessonId, cancellationToken);

        if (progression is null)
        {
            progression = new()
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

        var enrollment = await _dbContext.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.ModuleId == moduleId, cancellationToken);

        if (enrollment is null)
        {
            enrollment = new()
            {
                StudentId = studentId,
                ModuleId = moduleId,
                EnrollmentDate = DateTime.Now,
                IsCompleted = false
            };
            _dbContext.Enrollments.Add(enrollment);
        }

        var totalLessons = await _dbContext.Lessons.CountAsync(l => l.ModuleId == moduleId, cancellationToken);
        var completedLessons = await _dbContext.LessonProgressions
            .Where(lp => lp.StudentId == studentId && lp.IsRead)
            .Join(_dbContext.Lessons, lp => lp.LessonId, l => l.Id, (lp, l) => new { lp, l })
            .CountAsync(x => x.l.ModuleId == moduleId, cancellationToken);

        var completion = _progressService.CalculateCompletion(completedLessons, totalLessons);
        enrollment.IsCompleted = completion >= 100;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Details), new { id = moduleId, studentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadCertificate(int moduleId, CancellationToken cancellationToken = default)
    {
        var studentId = ResolveStudentId();
        if (studentId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Details), "Courses", new { id = moduleId }) });
        }

        var module = await _dbContext.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

        if (module is null)
        {
            return NotFound();
        }

        var enrollment = await _dbContext.Enrollments
            .FirstOrDefaultAsync(e => e.ModuleId == moduleId && e.StudentId == studentId, cancellationToken);

        if (enrollment is null || !enrollment.IsCompleted)
        {
            return BadRequest("Le module doit être terminé avant de générer un certificat.");
        }

        var certificate = await _dbContext.Certificates
            .FirstOrDefaultAsync(c => c.ModuleId == moduleId && c.StudentId == studentId, cancellationToken);

        if (certificate is null)
        {
            certificate = new()
            {
                ModuleId = moduleId,
                StudentId = studentId,
                UniqueCode = _certificateService.GenerateCertificateNumber(studentId, moduleId),
                IssueDate = DateTime.Now
            };

            _dbContext.Certificates.Add(certificate);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var pdfBytes = _certificateService.GenerateCertificatePdf(studentId, module.Title, certificate.UniqueCode, certificate.IssueDate);
        var fileName = $"certificate-{moduleId}-{studentId}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    private string? ResolveStudentId()
    {
        var currentUserName = HttpContext.Session.GetString("CurrentUserName");
        var role = HttpContext.Session.GetString("CurrentUserRole");

        if (!string.IsNullOrWhiteSpace(currentUserName)
            && string.Equals(role, "etudiant", StringComparison.OrdinalIgnoreCase))
        {
            return currentUserName;
        }

        return null;
    }
}