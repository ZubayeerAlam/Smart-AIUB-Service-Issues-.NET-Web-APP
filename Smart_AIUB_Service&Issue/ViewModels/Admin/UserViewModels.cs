using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smart_AIUB_Service_Issue.ViewModels.Admin;

public record UserListItem(
    string Id,
    string FullName,
    string Email,
    string? UniversityId,
    string? Role,
    string? DepartmentCode,
    string? TeamName,
    bool IsActive,
    bool IsLockedOut);

/// <summary>User list adds a role filter on top of search + status.</summary>
public class UserIndexViewModel : ListPageViewModel<UserListItem>
{
    public string? Role { get; set; }
    public string? CurrentUserId { get; set; }
}

/// <summary>Admin creates Support Staff and Admin accounts (Students/Faculty self-register).</summary>
public class CreateUserViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Employee ID (optional)")]
    public string? UniversityId { get; set; }

    /// <summary>SupportStaff or Admin. Checked again on the server.</summary>
    [Required(ErrorMessage = "Please select a role.")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Required when Role is SupportStaff (checked in the service).</summary>
    [Display(Name = "Support team")]
    public int? SupportTeamId { get; set; }

    [Required, StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Temporary password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [ValidateNever]
    public IEnumerable<SelectListItem> RoleOptions { get; set; } = [];

    [ValidateNever]
    public IEnumerable<SelectListItem> TeamOptions { get; set; } = [];
}

/// <summary>
/// Edit a user's profile. Email and role are shown but not editable here,
/// and are never read from the posted form.
/// </summary>
public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [ValidateNever]
    public string Email { get; set; } = string.Empty;

    [ValidateNever]
    public string Role { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Student / Employee ID")]
    public string? UniversityId { get; set; }

    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Support team")]
    public int? SupportTeamId { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = [];

    [ValidateNever]
    public IEnumerable<SelectListItem> TeamOptions { get; set; } = [];
}

public class ResetPasswordViewModel
{
    public string Id { get; set; } = string.Empty;

    [ValidateNever]
    public string DisplayName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
