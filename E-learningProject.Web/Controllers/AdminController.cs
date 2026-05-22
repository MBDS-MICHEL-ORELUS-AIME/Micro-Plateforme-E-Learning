using E_learningProject.Data.Context;
using E_learningProject.Web.Models;
using E_learningProject.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Web.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private static readonly int[] AllowedPeriods = [7, 30, 90];
    private const string SuperAdminRole = "superadmin";

    public AdminController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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
}