using E_learningProject.Data.Context;
using E_learningProject.Services.Interfaces;
using E_learningProject.Web.Models;
using E_learningProject.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace E_learningProject.Web.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOpenContentImportService _openContentImportService;
    private static readonly int[] AllowedPeriods = [7, 30, 90];
    private const string SuperAdminRole = "superadmin";

    public AdminController(ApplicationDbContext dbContext, IOpenContentImportService openContentImportService)
    {
        _dbContext = dbContext;
        _openContentImportService = openContentImportService;
    }

    public async Task<IActionResult> Dashboard(int days = 30, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdmin())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Dashboard), "Admin") });
        }

        if (!AllowedPeriods.Contains(days))
        {
            days = 30;
        }

        var sinceDate = DateTime.UtcNow.AddDays(-days);

        var totalModules = await _dbContext.Modules.CountAsync(cancellationToken);
        var totalLessons = await _dbContext.Lessons.CountAsync(cancellationToken);
        var totalQuizzes = await _dbContext.Quizzes.CountAsync(cancellationToken);
        var totalEnrollments = await _dbContext.Enrollments.CountAsync(e => e.EnrollmentDate >= sinceDate, cancellationToken);
        var completedEnrollments = await _dbContext.Enrollments.CountAsync(e => e.IsCompleted && e.EnrollmentDate >= sinceDate, cancellationToken);
        var certificatesIssued = await _dbContext.Certificates.CountAsync(c => c.IssueDate >= sinceDate, cancellationToken);
        var quizAttempts = await _dbContext.QuizResults.CountAsync(r => r.AttemptDate >= sinceDate, cancellationToken);
        var passedAttempts = await _dbContext.QuizResults.CountAsync(r => r.IsPassed && r.AttemptDate >= sinceDate, cancellationToken);

        var recentModules = await _dbContext.Modules
            .AsNoTracking()
            .OrderByDescending(m => m.Id)
            .Take(5)
            .Select(m => new ModuleOverviewItem
            {
                Title = m.Title,
                LessonCount = m.Lessons.Count,
                HasFinalQuiz = m.QuizId != null
            })
            .ToListAsync(cancellationToken);

        var recentQuizAttempts = await _dbContext.QuizResults
            .AsNoTracking()
            .OrderByDescending(r => r.AttemptDate)
            .Take(5)
            .Select(r => new QuizAttemptItem
            {
                QuizTitle = r.Quiz != null ? r.Quiz.Title : $"Quiz #{r.QuizId}",
                StudentId = r.StudentId,
                Score = r.Score,
                IsPassed = r.IsPassed,
                AttemptDate = r.AttemptDate
            })
            .ToListAsync(cancellationToken);

        var recentDiscussionThreads = await _dbContext.DiscussionThreads
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new DiscussionThreadItem
            {
                Title = t.Title,
                StudentId = t.StudentId,
                ReplyCount = t.Replies.Count,
                IsResolved = t.IsResolved,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var usersByRole = await _dbContext.AppUsers
            .AsNoTracking()
            .Join(_dbContext.AppRoles, u => u.RoleId, r => r.Id, (u, r) => r.Name)
            .GroupBy(name => name)
            .Select(group => new UserRoleBreakdownItem
            {
                RoleName = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var recentCertificates = await _dbContext.Certificates
            .AsNoTracking()
            .Include(c => c.Module)
            .OrderByDescending(c => c.IssueDate)
            .Take(8)
            .Select(c => new CertificateIssueItem
            {
                StudentId = c.StudentId,
                ModuleTitle = c.Module != null ? c.Module.Title : $"Module #{c.ModuleId}",
                IssueDate = c.IssueDate,
                UniqueCode = c.UniqueCode
            })
            .ToListAsync(cancellationToken);

        var enrollmentByModule = await _dbContext.Enrollments
            .AsNoTracking()
            .Where(e => e.EnrollmentDate >= sinceDate)
            .Join(_dbContext.Modules, e => e.ModuleId, m => m.Id, (e, m) => m.Title)
            .GroupBy(title => title)
            .Select(group => new
            {
                ModuleTitle = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(cancellationToken);

        var quizPassRateByQuiz = await _dbContext.QuizResults
            .AsNoTracking()
            .Where(r => r.AttemptDate >= sinceDate)
            .Join(_dbContext.Quizzes, r => r.QuizId, q => q.Id, (r, q) => new { q.Title, r.IsPassed })
            .GroupBy(x => x.Title)
            .Select(group => new
            {
                QuizTitle = group.Key,
                PassRate = group.Any() ? group.Count(x => x.IsPassed) * 100.0 / group.Count() : 0
            })
            .OrderByDescending(x => x.PassRate)
            .Take(8)
            .ToListAsync(cancellationToken);

        var viewModel = new AdminDashboardViewModel
        {
            IsSuperAdminView = IsSuperAdmin(),
            SelectedPeriodDays = days,
            TotalModules = totalModules,
            TotalLessons = totalLessons,
            TotalQuizzes = totalQuizzes,
            TotalEnrollments = totalEnrollments,
            CompletedEnrollments = completedEnrollments,
            CertificatesIssued = certificatesIssued,
            QuizAttempts = quizAttempts,
            QuizPassRate = quizAttempts == 0 ? 0 : Math.Round((double)passedAttempts / quizAttempts * 100, 2),
            RecentModules = recentModules,
            RecentQuizAttempts = recentQuizAttempts,
            RecentDiscussionThreads = recentDiscussionThreads,
            UsersByRole = usersByRole,
            RecentCertificates = recentCertificates,
            EnrollmentChartLabels = enrollmentByModule.Select(x => x.ModuleTitle).ToList(),
            EnrollmentChartValues = enrollmentByModule.Select(x => x.Count).ToList(),
            QuizChartLabels = quizPassRateByQuiz.Select(x => x.QuizTitle).ToList(),
            QuizChartValues = quizPassRateByQuiz.Select(x => Math.Round(x.PassRate, 2)).ToList()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Users(CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdmin())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Users), "Admin") });
        }

        var users = await _dbContext.AppUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.Id)
            .Select(u => new AdminUserItemViewModel
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                RoleName = u.Role != null ? u.Role.Name : "N/A"
            })
            .ToListAsync(cancellationToken);

        var viewModel = new AdminUsersViewModel
        {
            Users = users
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> CreateUser(CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdmin())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Users), "Admin") });
        }

        var model = new AdminUserUpsertViewModel
        {
            RoleOptions = await GetRoleOptions(cancellationToken)
        };

        return View("UserForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(AdminUserUpsertViewModel model, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdmin())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Users), "Admin") });
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Le mot de passe est requis pour un nouvel utilisateur.");
        }
        else if (model.Password.Trim().Length < 6)
        {
            ModelState.AddModelError(nameof(model.Password), "Le mot de passe doit contenir au moins 6 caractères.");
        }

        await ValidateRoleAuthorization(model.RoleId, null, cancellationToken);

        await ValidateUniqueConstraints(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            model.RoleOptions = await GetRoleOptions(cancellationToken);
            return View("UserForm", model);
        }

        var normalizedPassword = model.Password!.Trim();

        _dbContext.AppUsers.Add(new Core.Entities.User
        {
            UserName = model.UserName.Trim(),
            Email = model.Email.Trim(),
            PasswordHash = PasswordSecurity.Hash(normalizedPassword),
            RoleId = model.RoleId
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int id, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdmin())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Users), "Admin") });
        }

        var user = await _dbContext.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!IsSuperAdmin() && await IsSuperAdminRoleId(user.RoleId, cancellationToken))
        {
            return Forbid();
        }

        var model = new AdminUserUpsertViewModel
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            RoleId = user.RoleId,
            RoleOptions = await GetRoleOptions(cancellationToken)
        };

        return View("UserForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(AdminUserUpsertViewModel model, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdmin())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Users), "Admin") });
        }

        if (!model.Id.HasValue)
        {
            return BadRequest();
        }

        var user = await _dbContext.AppUsers.FirstOrDefaultAsync(u => u.Id == model.Id.Value, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!IsSuperAdmin() && await IsSuperAdminRoleId(user.RoleId, cancellationToken))
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(model.Password) && model.Password.Trim().Length < 6)
        {
            ModelState.AddModelError(nameof(model.Password), "Le mot de passe doit contenir au moins 6 caractères.");
        }

        await ValidateRoleAuthorization(model.RoleId, user.RoleId, cancellationToken);

        await ValidateUniqueConstraints(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            model.RoleOptions = await GetRoleOptions(cancellationToken);
            return View("UserForm", model);
        }

        user.UserName = model.UserName.Trim();
        user.Email = model.Email.Trim();
        user.RoleId = model.RoleId;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.PasswordHash = PasswordSecurity.Hash(model.Password);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdmin())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Users), "Admin") });
        }

        var user = await _dbContext.AppUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!IsSuperAdmin() && await IsSuperAdminRoleId(user.RoleId, cancellationToken))
        {
            return Forbid();
        }

        _dbContext.AppUsers.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> ContentSync(CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdmin())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(ContentSync), "Admin") });
        }

        await EnsureContentImportLogsTableAsync(cancellationToken);

        var sourceStats = await _dbContext.ContentImportLogs
            .AsNoTracking()
            .GroupBy(x => x.SourceName)
            .Select(group => new ImportSourceItemViewModel
            {
                SourceName = group.Key,
                ImportsCount = group.Count(),
                LastImportedAt = group.Max(x => x.ImportedAt)
            })
            .OrderByDescending(x => x.LastImportedAt)
            .ToListAsync(cancellationToken);

        var model = new AdminContentSyncViewModel
        {
            TotalImportedModules = sourceStats.Sum(x => x.ImportsCount),
            DistinctSources = sourceStats.Count,
            LastImportAt = sourceStats.Count == 0 ? null : sourceStats.Max(x => x.LastImportedAt),
            Sources = sourceStats
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunContentSync(int maxModules = 20, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdmin())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(ContentSync), "Admin") });
        }

        await EnsureContentImportLogsTableAsync(cancellationToken);

        var result = await _openContentImportService.ImportAsync(maxModules, cancellationToken);

        TempData["SyncSummary"] = $"Import termine: {result.ImportedModules} modules, {result.ImportedLessons} lecons, {result.ImportedQuizzes} quiz. Doublons ignores: {result.SkippedDuplicates}.";

        if (result.Errors.Count > 0)
        {
            TempData["SyncErrors"] = string.Join(" | ", result.Errors.Take(5));
        }

        return RedirectToAction(nameof(ContentSync));
    }

    [HttpGet]
    public async Task<IActionResult> LessonPdfSources(CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdmin())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(LessonPdfSources), "Admin") });
        }

        var lessons = await _dbContext.Lessons
            .AsNoTracking()
            .Include(l => l.Module)
            .OrderBy(l => l.Module!.Title)
            .ThenBy(l => l.Order)
            .Select(l => new AdminLessonPdfSourceItemViewModel
            {
                LessonId = l.Id,
                ModuleTitle = l.Module != null ? l.Module.Title : "Module inconnu",
                LessonTitle = l.Title,
                PdfPath = l.PdfPath,
                IsOpenSourcePdf = IsOpenSourcePdfPath(l.PdfPath),
                SourceLabel = ResolveSourceLabel(l.PdfPath)
            })
            .ToListAsync(cancellationToken);

        var model = new AdminLessonPdfSourcesViewModel
        {
            TotalLessons = lessons.Count,
            OpenSourcePdfCount = lessons.Count(l => l.IsOpenSourcePdf),
            NonOpenSourcePdfCount = lessons.Count(l => !l.IsOpenSourcePdf),
            Lessons = lessons
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshLessonPdfSources(CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdmin())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(LessonPdfSources), "Admin") });
        }

        var replacedCount = await ReplaceNonOpenLessonPdfsAsync(cancellationToken);
        TempData["PdfSourceSyncSummary"] = $"Remplacement terminé: {replacedCount} leçon(s) ont reçu un PDF de source ouverte.";
        return RedirectToAction(nameof(LessonPdfSources));
    }

    private async Task EnsureContentImportLogsTableAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS "ContentImportLogs" (
                "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                "EntityType" character varying(50) NOT NULL,
                "EntityId" integer NOT NULL,
                "SourceName" character varying(200) NOT NULL,
                "SourceUrl" character varying(1000) NOT NULL,
                "SourceLicense" character varying(200) NOT NULL,
                "ContentHash" character varying(128) NOT NULL,
                "ImportedAt" timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ContentImportLogs_ContentHash"
                ON "ContentImportLogs" ("ContentHash");

            CREATE INDEX IF NOT EXISTS "IX_ContentImportLogs_ImportedAt"
                ON "ContentImportLogs" ("ImportedAt");
            """;

        await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private async Task<int> ReplaceNonOpenLessonPdfsAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MicroLMS/1.0 (+academic project)");

        var lessons = await _dbContext.Lessons
            .Include(l => l.Module)
            .ToListAsync(cancellationToken);

        var replaced = 0;
        foreach (var lesson in lessons)
        {
            if (IsOpenSourcePdfPath(lesson.PdfPath))
            {
                continue;
            }

            var query = string.IsNullOrWhiteSpace(lesson.Module?.Title)
                ? lesson.Title
                : $"{lesson.Title} {lesson.Module!.Title}";

            var wikiPdfUrl = await TryResolveWikipediaPdfUrlAsync(httpClient, query);
            if (wikiPdfUrl is null)
            {
                wikiPdfUrl = await TryResolveWikipediaPdfUrlAsync(httpClient, lesson.Title);
            }

            if (wikiPdfUrl is null)
            {
                continue;
            }

            lesson.PdfPath = wikiPdfUrl;
            replaced++;
        }

        if (replaced > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return replaced;
    }

    private static async Task<string?> TryResolveWikipediaPdfUrlAsync(HttpClient httpClient, string query)
    {
        try
        {
            var searchEndpoint = $"https://fr.wikipedia.org/w/api.php?action=query&list=search&srlimit=1&format=json&srsearch={Uri.EscapeDataString(query)}";
            using var searchResponse = await httpClient.GetAsync(searchEndpoint);
            if (!searchResponse.IsSuccessStatusCode)
            {
                return null;
            }

            await using var searchStream = await searchResponse.Content.ReadAsStreamAsync(cancellationToken: default);
            using var searchDoc = await JsonDocument.ParseAsync(searchStream);

            var rootSearch = searchDoc.RootElement;
            if (!rootSearch.TryGetProperty("query", out var queryElement)
                || !queryElement.TryGetProperty("search", out var searchArray)
                || searchArray.GetArrayLength() == 0)
            {
                return null;
            }

            var pageTitle = searchArray[0].GetProperty("title").GetString();
            if (string.IsNullOrWhiteSpace(pageTitle))
            {
                return null;
            }

            return $"https://fr.wikipedia.org/api/rest_v1/page/pdf/{Uri.EscapeDataString(pageTitle)}";
        }
        catch
        {
            return null;
        }
    }

    private static bool IsOpenSourcePdfPath(string? pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            return false;
        }

        return pdfPath.Contains("fr.wikipedia.org/api/rest_v1/page/pdf/", StringComparison.OrdinalIgnoreCase)
            || pdfPath.Contains("wikipedia.org", StringComparison.OrdinalIgnoreCase)
            || pdfPath.Contains("wikiversity.org", StringComparison.OrdinalIgnoreCase)
            || pdfPath.Contains("wikimedia.org", StringComparison.OrdinalIgnoreCase)
            || pdfPath.Contains("gutenberg.org", StringComparison.OrdinalIgnoreCase)
            || pdfPath.Contains("openedition.org", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveSourceLabel(string? pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            return "Aucune source";
        }

        if (Uri.TryCreate(pdfPath, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }

        return "Source locale";
    }

    private bool CanAccessAdmin()
    {
        var role = HttpContext.Session.GetString("CurrentUserRole");
        return string.Equals(role, "coordinateur", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "superadmin", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<AdminRoleOptionViewModel>> GetRoleOptions(CancellationToken cancellationToken)
    {
        var roles = _dbContext.AppRoles.AsNoTracking();

        if (!IsSuperAdmin())
        {
            roles = roles.Where(r => r.Name != SuperAdminRole);
        }

        return await roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new AdminRoleOptionViewModel { Id = r.Id, Name = r.Name })
            .ToListAsync(cancellationToken);
    }

    private bool IsSuperAdmin()
    {
        var role = HttpContext.Session.GetString("CurrentUserRole");
        return string.Equals(role, SuperAdminRole, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> IsSuperAdminRoleId(int roleId, CancellationToken cancellationToken)
    {
        return await _dbContext.AppRoles
            .AsNoTracking()
            .AnyAsync(r => r.Id == roleId && r.Name == SuperAdminRole, cancellationToken);
    }

    private async Task ValidateRoleAuthorization(int selectedRoleId, int? existingRoleId, CancellationToken cancellationToken)
    {
        if (IsSuperAdmin())
        {
            return;
        }

        if (await IsSuperAdminRoleId(selectedRoleId, cancellationToken))
        {
            ModelState.AddModelError(nameof(AdminUserUpsertViewModel.RoleId), "Seul un superadmin peut attribuer le rôle superadmin.");
            return;
        }

        if (existingRoleId.HasValue && await IsSuperAdminRoleId(existingRoleId.Value, cancellationToken))
        {
            ModelState.AddModelError(nameof(AdminUserUpsertViewModel.RoleId), "Seul un superadmin peut modifier un compte superadmin.");
        }
    }

    private async Task ValidateUniqueConstraints(AdminUserUpsertViewModel model, CancellationToken cancellationToken)
    {
        var normalizedUserName = model.UserName.Trim();
        var normalizedEmail = model.Email.Trim();

        var usernameExists = await _dbContext.AppUsers
            .AnyAsync(u => u.UserName == normalizedUserName && u.Id != (model.Id ?? 0), cancellationToken);
        if (usernameExists)
        {
            ModelState.AddModelError(nameof(model.UserName), "Ce nom d'utilisateur existe déjà.");
        }

        var emailExists = await _dbContext.AppUsers
            .AnyAsync(u => u.Email == normalizedEmail && u.Id != (model.Id ?? 0), cancellationToken);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Cette adresse e-mail existe déjà.");
        }

        var roleExists = await _dbContext.AppRoles.AnyAsync(r => r.Id == model.RoleId, cancellationToken);
        if (!roleExists)
        {
            ModelState.AddModelError(nameof(model.RoleId), "Veuillez sélectionner un rôle valide.");
        }
    }
    public async Task<IActionResult> Moderation(string filter = "pending", CancellationToken cancellationToken = default)
    {
        var role = HttpContext.Session.GetString("CurrentUserRole");
        if (!string.Equals(role, "coordinateur", StringComparison.OrdinalIgnoreCase) && !string.Equals(role, "superadmin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var reports = await _dbContext.DiscussionReports.AsNoTracking()
            .Include(r => r.Thread)
            .OrderByDescending(r => r.ReportedAt)
            .Select(r => new ModerationReportItemViewModel
            {
                ReportId = r.Id, ThreadId = r.ThreadId,
                ThreadTitle = r.Thread != null ? r.Thread.Title : string.Empty,
                ReporterStudentId = r.ReporterStudentId, Reason = r.Reason,
                ReportedAt = r.ReportedAt, IsHandled = r.IsHandled, HandlerNote = r.HandlerNote
            })
            .ToListAsync(cancellationToken);

        var filtered = filter == "handled" ? reports.Where(r => r.IsHandled).ToList() : reports.Where(r => !r.IsHandled).ToList();

        var vm = new ModerationIndexViewModel
        {
            PendingCount = reports.Count(r => !r.IsHandled),
            HandledCount = reports.Count(r => r.IsHandled),
            Reports = filtered
        };
        ViewData["Filter"] = filter;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HandleReport(int reportId, string action, CancellationToken cancellationToken = default)
    {
        var role = HttpContext.Session.GetString("CurrentUserRole");
        if (!string.Equals(role, "coordinateur", StringComparison.OrdinalIgnoreCase) && !string.Equals(role, "superadmin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var report = await _dbContext.DiscussionReports.Include(r => r.Thread).FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        if (report is null) return NotFound();

        if (action == "delete_thread" && report.Thread is not null)
        {
            _dbContext.DiscussionThreads.Remove(report.Thread);
            report.IsHandled = true;
            report.HandlerNote = "Fil supprime par le moderateur.";
        }
        else
        {
            report.IsHandled = true;
            report.HandlerNote = "Signalement rejete.";
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Moderation));
    }
}