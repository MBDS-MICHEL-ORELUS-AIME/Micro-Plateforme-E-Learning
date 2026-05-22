using E_learningProject.Data.Context;
using E_learningProject.Web.Models;
using E_learningProject.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private const string PublicRegistrationDefaultRole = "etudiant";

    public AccountController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("CurrentUserName")))
        {
            return RedirectToRoleHome(HttpContext.Session.GetString("CurrentUserRole"));
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _dbContext.AppUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserName == model.UserName, cancellationToken);

        if (user is null || !PasswordSecurity.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Nom d'utilisateur ou mot de passe invalide.");
            return View(model);
        }

        // Upgrade legacy plain-text passwords on successful login.
        if (!PasswordSecurity.IsHashed(user.PasswordHash))
        {
            user.PasswordHash = PasswordSecurity.Hash(model.Password);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        HttpContext.Session.SetString("CurrentUserId", user.Id.ToString());
        HttpContext.Session.SetString("CurrentUserName", user.UserName);
        HttpContext.Session.SetString("CurrentUserRole", user.Role?.Name ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToRoleHome(user.Role?.Name);
    }

    [HttpGet]
    public async Task<IActionResult> Register(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("CurrentUserName")))
        {
            return RedirectToRoleHome(HttpContext.Session.GetString("CurrentUserRole"));
        }

        var defaultRoleId = await _dbContext.AppRoles
            .AsNoTracking()
            .Where(r => r.Name == PublicRegistrationDefaultRole)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!defaultRoleId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Le rôle par défaut 'etudiant' est introuvable.");
        }

        var viewModel = new RegisterViewModel
        {
            ReturnUrl = returnUrl,
            RoleId = defaultRoleId
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userName = model.UserName.Trim();
        var email = model.Email.Trim();

        var role = await _dbContext.AppRoles
            .FirstOrDefaultAsync(r => r.Name == PublicRegistrationDefaultRole, cancellationToken);
        if (role is null)
        {
            ModelState.AddModelError(string.Empty, "Le rôle par défaut 'etudiant' est introuvable.");
            return View(model);
        }

        if (await _dbContext.AppUsers.AnyAsync(u => u.UserName == userName, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.UserName), "Ce nom d'utilisateur existe déjà.");
            return View(model);
        }

        if (await _dbContext.AppUsers.AnyAsync(u => u.Email == email, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Email), "Cette adresse e-mail existe déjà.");
            return View(model);
        }

        var user = new Core.Entities.User
        {
            UserName = userName,
            Email = email,
            PasswordHash = PasswordSecurity.Hash(model.Password),
            RoleId = role.Id
        };

        _dbContext.AppUsers.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        HttpContext.Session.SetString("CurrentUserId", user.Id.ToString());
        HttpContext.Session.SetString("CurrentUserName", user.UserName);
        HttpContext.Session.SetString("CurrentUserRole", role.Name);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToRoleHome(role.Name);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("CurrentUserId");
        HttpContext.Session.Remove("CurrentUserName");
        HttpContext.Session.Remove("CurrentUserRole");
        return RedirectToAction("Public", "Home");
    }

    private IActionResult RedirectToRoleHome(string? role)
    {
        if (string.Equals(role, "etudiant", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Dashboard", "Learner");
        }

        if (string.Equals(role, "enseignant", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Workspace", "Teacher");
        }

        if (string.Equals(role, "coordinateur", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        if (string.Equals(role, "superadmin", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        return RedirectToAction("Index", "Courses");
    }

}
