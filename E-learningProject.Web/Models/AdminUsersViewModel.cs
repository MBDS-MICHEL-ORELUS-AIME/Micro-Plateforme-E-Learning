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
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Le mot de passe et sa confirmation ne correspondent pas.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public int? RoleId { get; set; }

    public string? ReturnUrl { get; set; }
    public List<AdminRoleOptionViewModel> RoleOptions { get; set; } = new();
}
