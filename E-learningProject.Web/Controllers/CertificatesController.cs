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
        // La vérification permet à n'importe qui de contrôler l'authenticité d'un certificat.
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
            .Include(c => c.Student)
            .FirstOrDefaultAsync(c => c.UniqueCode == normalizedCode, cancellationToken);

        if (certificate is null)
        {
            viewModel.IsValid = false;
            return View(viewModel);
        }

        viewModel.IsValid = true;
        viewModel.CertificateCode = certificate.UniqueCode;
        viewModel.StudentId = certificate.Student?.UserName ?? certificate.UserId.ToString();
        viewModel.ModuleTitle = certificate.Module?.Title;
        viewModel.IssueDate = certificate.IssueDate;

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
<<<<<<< HEAD
        var userId = ResolveStudentId();
        if (userId is null)
=======
        // La page d'accueil certificat montre ceux déjà obtenus et ceux encore disponibles.
        var studentId = ResolveStudentId();
        if (studentId is null)
>>>>>>> 860b19364261ed14ea4aea4c4dd58788263dcf9d
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "Certificates") });
        }
        var userName = HttpContext.Session.GetString("CurrentUserName") ?? userId.ToString()!;

        var certificates = await _dbContext.Certificates
            .AsNoTracking()
            .Include(c => c.Module)
            .Where(c => c.UserId == userId)
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
            .Where(r => r.UserId == userId && r.IsPassed)
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
            StudentId = userName,
            ExistingCertificates = existingCertificates,
            AvailableCertificates = availableCertificates
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Download(int moduleId, CancellationToken cancellationToken = default)
    {
<<<<<<< HEAD
        var userId = ResolveStudentId();
        if (userId is null)
=======
        // Avant génération, on vérifie que le module est bien associé à un quiz réussi.
        var studentId = ResolveStudentId();
        if (studentId is null)
>>>>>>> 860b19364261ed14ea4aea4c4dd58788263dcf9d
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "Certificates") });
        }
        var userName = HttpContext.Session.GetString("CurrentUserName") ?? userId.ToString()!;

        var module = await _dbContext.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

        if (module is null)
        {
            return NotFound();
        }

        var certificate = await _dbContext.Certificates
            .FirstOrDefaultAsync(c => c.ModuleId == moduleId && c.UserId == userId, cancellationToken);

        var canGenerate = certificate != null;
        if (!canGenerate && module.QuizId.HasValue)
        {
            canGenerate = await _dbContext.QuizResults
                .AsNoTracking()
                .AnyAsync(r => r.UserId == userId && r.QuizId == module.QuizId && r.IsPassed, cancellationToken);
        }

        if (!canGenerate)
        {
            return BadRequest("Le certificat ne peut être généré que pour un module lié à un quiz réussi.");
        }

        var viewModel = new CertificateDownloadConfirmationViewModel
        {
            ModuleId = module.Id,
            ModuleTitle = module.Title,
            RecipientName = userName
        };

        return View(viewModel);
    }

    [HttpPost]
    [ActionName("Download")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadConfirmed(int moduleId, CancellationToken cancellationToken = default)
    {
<<<<<<< HEAD
        var userId = ResolveStudentId();
        if (userId is null)
=======
        // La version confirmée génère le PDF uniquement après validation des conditions métier.
        var studentId = ResolveStudentId();
        if (studentId is null)
>>>>>>> 860b19364261ed14ea4aea4c4dd58788263dcf9d
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "Certificates") });
        }
        var userName = HttpContext.Session.GetString("CurrentUserName") ?? userId.ToString()!;

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
            .AnyAsync(r => r.UserId == userId && r.QuizId == module.QuizId.Value && r.IsPassed, cancellationToken);

        if (!hasPassedQuiz)
        {
            return BadRequest("Le quiz lié à ce module doit être réussi avant de générer un certificat.");
        }

        var certificate = await _dbContext.Certificates
            .FirstOrDefaultAsync(c => c.ModuleId == moduleId && c.UserId == userId, cancellationToken);

        if (certificate is null)
        {
            certificate = new Core.Entities.Certificate
            {
                ModuleId = module.Id,
                UserId = userId.Value,
                UniqueCode = _certificateService.GenerateCertificateNumber(userName, module.Id),
                IssueDate = DateTime.UtcNow
            };

            _dbContext.Certificates.Add(certificate);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var pdfBytes = _certificateService.GenerateCertificatePdf(userName, module.Title, certificate.UniqueCode, certificate.IssueDate);
        var fileName = $"certificate-{module.Id}-{userName}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    private int? ResolveStudentId()
    {
<<<<<<< HEAD
=======
        // On cherche d'abord le nom complet en session, puis dans la base si nécessaire.
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
>>>>>>> 860b19364261ed14ea4aea4c4dd58788263dcf9d
        var role = HttpContext.Session.GetString("CurrentUserRole");
        var userIdStr = HttpContext.Session.GetString("CurrentUserId");

        if (!string.IsNullOrWhiteSpace(userIdStr)
            && int.TryParse(userIdStr, out var userId)
            && string.Equals(role, "etudiant", StringComparison.OrdinalIgnoreCase))
        {
            return userId;
        }

        return null;
    }
}
