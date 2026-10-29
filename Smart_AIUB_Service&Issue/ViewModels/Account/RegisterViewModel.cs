using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smart_AIUB_Service_Issue.ViewModels.Account;

/// <summary>
/// Data the Register form posts. Only these fields can be bound from the request,
/// so a user cannot sneak in e.g. IsActive=false or SupportTeamId.
/// </summary>
public class RegisterViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Student / Employee ID")]
    public string UniversityId { get; set; } = string.Empty;

    /// <summary>Must be Student or Faculty. Checked again on the server in AccountService.</summary>
    [Required(ErrorMessage = "Please select Student or Faculty.")]
    [Display(Name = "I am a")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select your department.")]
    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Required, StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Dropdown options: filled by the controller, never posted back.
    [ValidateNever]
    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = [];

    [ValidateNever]
    public IEnumerable<SelectListItem> RoleOptions { get; set; } = [];
}
