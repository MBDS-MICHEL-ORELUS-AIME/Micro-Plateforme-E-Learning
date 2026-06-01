using E_learningProject.Data.Context;
using E_learningProject.Services.Interfaces;
using E_learningProject.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Web.Controllers;

public class CertificatesController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICertificateService _certificateService;

    public CertificatesController(ApplicationDbContext dbContext, ICertificateService certificateService)
    {
        _dbContext = dbContext;
        _certificateService = certificateService;
    }

    [HttpGet]
    public async Task<IActionResult> Verify(string? code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(code)
            ? null
            : code.Trim();

        var viewModel = new CertificateVerificationViewModel
        {
            SearchCode = normalizedCode,
            Searched = !string.IsNullOrWhiteSpace(normalizedCode)
        };

        if (!viewModel.Searched)
        {
            return View(viewModel);
        }

        var certificate = await _dbContext.Certificates
            .AsNoTracking()
            .Include(c => c.Module)
            .FirstOrDefaultAsync(c => c.UniqueCode == normalizedCode, cancellationToken);

        if (certificate is null)
        {
            viewModel.IsValid = false;
            return View(viewModel);
        }

        viewModel.IsValid = true;
        viewModel.CertificateCode = certificate.UniqueCode;
        viewModel.StudentId = certificate.StudentId;
        viewModel.ModuleTitle = certificate.Module?.Title;
        viewModel.IssueDate = certificate.IssueDate;

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var studentId = ResolveStudentId();
        if (studentId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "Certificates") });
        }

        var certificates = await _dbContext.Certificates
            .AsNoTracking()
            .Include(c => c.Module)
            .Where(c => c.StudentId == studentId)
            .ToListAsync(cancellationToken);

        var existingCertificates = certificates.Select(c => new LearnerCertificateItemViewModel
        {
            ModuleId = c.ModuleId,
            ModuleTitle = c.Module?.Title ?? "Module inconnu",
            CertificateCode = c.UniqueCode,
            IssueDate = c.IssueDate,
            HasCertificate = true
        }).ToList();

        var passedQuizIds = await _dbContext.QuizResults
            .AsNoTracking()
            .Where(r => r.StudentId == studentId && r.IsPassed)
            .Select(r => r.QuizId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var pendingModules = await _dbContext.Modules
            .AsNoTracking()
            .Where(m => m.QuizId != null && passedQuizIds.Contains(m.QuizId.Value))
            .ToListAsync(cancellationToken);

        var existingModuleIds = new HashSet<int>(existingCertificates.Select(c => c.ModuleId));

        var availableCertificates = pendingModules
            .Where(m => !existingModuleIds.Contains(m.Id))
            .Select(m => new LearnerCertificateItemViewModel
            {
                ModuleId = m.Id,
                ModuleTitle = m.Title,
                HasCertificate = false
            })
            .ToList();

        var viewModel = new LearnerCertificatesViewModel
        {
            StudentId = studentId,
            ExistingCertificates = existingCertificates,
            AvailableCertificates = availableCertificates
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Download(int moduleId, CancellationToken cancellationToken = default)
    {
        var studentId = ResolveStudentId();
        if (studentId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "Certificates") });
        }

        var module = await _dbContext.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

        if (module is null)
        {
            return NotFound();
        }

        if (!module.QuizId.HasValue)
        {
            return BadRequest("Le certificat ne peut être généré que pour un module lié à un quiz réussi.");
        }

        var hasPassedQuiz = await _dbContext.QuizResults
            .AsNoTracking()
            .AnyAsync(r => r.StudentId == studentId && r.QuizId == module.QuizId.Value && r.IsPassed, cancellationToken);

        if (!hasPassedQuiz)
        {
            return BadRequest("Le quiz lié à ce module doit être réussi avant de générer un certificat.");
        }

        var recipientName = await ResolveCertificateRecipientName(studentId, cancellationToken);
        var viewModel = new CertificateDownloadConfirmationViewModel
        {
            ModuleId = module.Id,
            ModuleTitle = module.Title,
            RecipientName = recipientName
        };

        return View(viewModel);
    }

    [HttpPost]
    [ActionName("Download")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadConfirmed(int moduleId, CancellationToken cancellationToken = default)
    {
        var studentId = ResolveStudentId();
        if (studentId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "Certificates") });
        }

        var module = await _dbContext.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

        if (module is null)
        {
            return NotFound();
        }

        if (!module.QuizId.HasValue)
        {
            return BadRequest("Le certificat ne peut être généré que pour un module lié à un quiz réussi.");
        }

        var hasPassedQuiz = await _dbContext.QuizResults
            .AsNoTracking()
            .AnyAsync(r => r.StudentId == studentId && r.QuizId == module.QuizId.Value && r.IsPassed, cancellationToken);

        if (!hasPassedQuiz)
        {
            return BadRequest("Le quiz lié à ce module doit être réussi avant de générer un certificat.");
        }

        var certificate = await _dbContext.Certificates
            .FirstOrDefaultAsync(c => c.ModuleId == moduleId && c.StudentId == studentId, cancellationToken);

        if (certificate is null)
        {
            certificate = new Core.Entities.Certificate
            {
                ModuleId = module.Id,
                StudentId = studentId,
                UniqueCode = _certificateService.GenerateCertificateNumber(studentId, module.Id),
                IssueDate = DateTime.UtcNow
            };

            _dbContext.Certificates.Add(certificate);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var recipientName = await ResolveCertificateRecipientName(studentId, cancellationToken);
        var pdfBytes = _certificateService.GenerateCertificatePdf(recipientName, module.Title, certificate.UniqueCode, certificate.IssueDate);
        var fileName = $"certificate-{certificate.UniqueCode}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    private async Task<string> ResolveCertificateRecipientName(string studentId, CancellationToken cancellationToken)
    {
        string? fullName = null;

        var currentUserIdRaw = HttpContext.Session.GetString("CurrentUserId");
        if (int.TryParse(currentUserIdRaw, out var currentUserId))
        {
            fullName = await _dbContext.AppUsers
                .AsNoTracking()
                .Where(u => u.Id == currentUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = await _dbContext.AppUsers
                .AsNoTracking()
                .Where(u => u.UserName == studentId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return string.IsNullOrWhiteSpace(fullName)
            ? "Apprenant"
            : fullName.Trim();
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
