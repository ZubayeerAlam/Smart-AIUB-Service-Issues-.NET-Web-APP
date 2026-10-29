using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smart_AIUB_Service_Issue.ViewModels.Admin;

public record IssueCategoryListItem(int Id, string Name, string? Description, string? DefaultTeamName, bool IsActive);

/// <summary>Add/Edit form. Id = 0 means "new".</summary>
public class IssueCategoryFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Default support team (optional)")]
    public int? DefaultSupportTeamId { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> TeamOptions { get; set; } = [];
}
