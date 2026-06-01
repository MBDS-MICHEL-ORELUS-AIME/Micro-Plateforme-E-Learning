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

        var certificate = await _dbContext.Certificates
            .FirstOrDefaultAsync(c => c.ModuleId == moduleId && c.StudentId == studentId, cancellationToken);

        var canGenerate = certificate != null;
        if (!canGenerate && module.QuizId.HasValue)
        {
            canGenerate = await _dbContext.QuizResults
                .AsNoTracking()
                .AnyAsync(r => r.StudentId == studentId && r.QuizId == module.QuizId && r.IsPassed, cancellationToken);
        }

        if (!canGenerate)
        {
            return BadRequest("Le certificat ne peut être généré que pour un module lié à un quiz réussi.");
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
    public async Task<IActionResult> DownloadConfirmed(int moduleId, string? recipientName, CancellationToken cancellationToken = default)
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

        var certificate = await _dbContext.Certificates
            .FirstOrDefaultAsync(c => c.ModuleId == moduleId && c.StudentId == studentId, cancellationToken);

        var canGenerate = certificate != null;
        if (!canGenerate && module.QuizId.HasValue)
        {
            canGenerate = await _dbContext.QuizResults
                .AsNoTracking()
                .AnyAsync(r => r.StudentId == studentId && r.QuizId == module.QuizId && r.IsPassed, cancellationToken);
        }

        if (!canGenerate)
        {
            return BadRequest("Le certificat ne peut être généré que pour un module lié à un quiz réussi.");
        }

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

        var resolvedRecipientName = await PersistCertificateRecipientName(studentId, recipientName, cancellationToken);
        var pdfBytes = _certificateService.GenerateCertificatePdf(resolvedRecipientName, module.Title, certificate.UniqueCode, certificate.IssueDate);
        var fileName = $"certificate-{module.Id}-{studentId}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    private async Task<string> ResolveCertificateRecipientName(string studentId, CancellationToken cancellationToken)
    {
        var fullName = await _dbContext.AppUsers
            .AsNoTracking()
            .Where(u => u.UserName == studentId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(fullName)
            ? BuildDisplayNameFromUserName(studentId)
            : fullName.Trim();
    }

    private async Task<string> PersistCertificateRecipientName(string studentId, string? recipientName, CancellationToken cancellationToken)
    {
        var normalizedRecipientName = string.IsNullOrWhiteSpace(recipientName)
            ? null
            : recipientName.Trim();

        if (string.IsNullOrWhiteSpace(normalizedRecipientName))
        {
            return await ResolveCertificateRecipientName(studentId, cancellationToken);
        }

        var user = await _dbContext.AppUsers.FirstOrDefaultAsync(u => u.UserName == studentId, cancellationToken);
        if (user is not null && !string.Equals(user.FullName?.Trim(), normalizedRecipientName, StringComparison.Ordinal))
        {
            user.FullName = normalizedRecipientName;
            await _dbContext.SaveChangesAsync(cancellationToken);
            HttpContext.Session.SetString("CurrentUserFullName", normalizedRecipientName);
        }

        return normalizedRecipientName;
    }

    private static string BuildDisplayNameFromUserName(string userName)
    {
        var normalized = (userName ?? string.Empty)
            .Trim()
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Apprenant";
        }

        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var formattedParts = new List<string>(parts.Length);

        foreach (var part in parts)
        {
            if (part.Length == 1)
            {
                formattedParts.Add(part.ToUpperInvariant());
                continue;
            }

            formattedParts.Add(char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant());
        }

        return string.Join(' ', formattedParts);
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
