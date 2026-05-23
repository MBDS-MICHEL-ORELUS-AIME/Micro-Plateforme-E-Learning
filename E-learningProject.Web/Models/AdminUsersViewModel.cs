namespace E_learningProject.Web.Models;

using System.ComponentModel.DataAnnotations;

public class AdminUsersViewModel
{
    public List<AdminUserItemViewModel> Users { get; set; } = new();
}

public class AdminUserItemViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

public class AdminUserUpsertViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Password { get; set; }

    [Required]
    public int RoleId { get; set; }

    public List<AdminRoleOptionViewModel> RoleOptions { get; set; } = new();
}

public class AdminRoleOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Le nom d'utilisateur est requis.")]
    [Display(Name = "Nom d'utilisateur")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est requis.")]
    [Display(Name = "Mot de passe")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Le nom d'utilisateur est requis.")]
    [StringLength(100, ErrorMessage = "Le nom d'utilisateur ne peut pas depasser 100 caracteres.")]
    [Display(Name = "Nom d'utilisateur")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'adresse e-mail est requise.")]
    [EmailAddress(ErrorMessage = "Le format de l'adresse e-mail est invalide.")]
    [StringLength(200, ErrorMessage = "L'adresse e-mail ne peut pas depasser 200 caracteres.")]
    [Display(Name = "Adresse e-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est requis.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir entre 6 et 100 caracteres.")]
    [Display(Name = "Mot de passe")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmation du mot de passe est requise.")]
    [Display(Name = "Confirmation du mot de passe")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Le mot de passe et sa confirmation ne correspondent pas.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public int? RoleId { get; set; }

    public string? ReturnUrl { get; set; }
    public List<AdminRoleOptionViewModel> RoleOptions { get; set; } = new();
}
