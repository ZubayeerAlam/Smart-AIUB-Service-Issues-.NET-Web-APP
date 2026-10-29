using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.ViewModels.Issues;

/// <summary>
/// The "Report an issue" form (also used for editing). Only these fields can be posted,
/// so a user cannot set Status, Priority, team, etc.
/// </summary>
public class IssueFormViewModel
{
    /// <summary>0 = new issue.</summary>
    public int Id { get; set; }

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Please select a building.")]
    [Display(Name = "Building")]
    public int? BuildingId { get; set; }

    [Required(ErrorMessage = "Please select a room.")]
    [Display(Name = "Floor & room")]
    public int? RoomId { get; set; }

    [Required, StringLength(100, MinimumLength = 5)]
    [Display(Name = "Short title")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 10)]
    [Display(Name = "What is the problem?")]
    public string Description { get; set; } = string.Empty;

    [ValidateNever]
    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = [];

    [ValidateNever]
    public IEnumerable<SelectListItem> BuildingOptions { get; set; } = [];

    /// <summary>Rooms of the selected building (filled again when the form is redisplayed).</summary>
    [ValidateNever]
    public IEnumerable<SelectListItem> RoomOptions { get; set; } = [];
}

/// <summary>One option in the cascading Room dropdown (returned as JSON).</summary>
public record RoomOption(int Id, string Text);

/// <summary>One row in any issue list. ReporterName is null on the public list.</summary>
public record IssueListItem(
    int Id,
    string Title,
    string CategoryName,
    string BuildingName,
    string RoomNumber,
    IssueStatus Status,
    IssuePriority? Priority,
    DateTime CreatedAt,
    string? ReporterName)
{
    public string TicketNumber => IssueDisplay.TicketNumber(Id);
}

public class IssueListViewModel
{
    public string? Search { get; set; }
    public IssueStatus? Status { get; set; }
    public PagedResult<IssueListItem> Results { get; set; } = null!;
}

public record IssueHistoryItem(IssueStatus? FromStatus, IssueStatus ToStatus, string ChangedByName, DateTime ChangedAt, string? Note);

public class IssueDetailsViewModel
{
    public int Id { get; set; }
    public string TicketNumber => IssueDisplay.TicketNumber(Id);
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string BuildingName { get; set; } = "";
    public string RoomNumber { get; set; } = "";
    public int Floor { get; set; }
    public string? RoomName { get; set; }
    public IssueStatus Status { get; set; }
    public IssuePriority? Priority { get; set; }
    public string ReporterName { get; set; } = "";
    public string ReporterEmail { get; set; } = "";
    public string? TeamName { get; set; }
    public string? StaffName { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<IssueHistoryItem> History { get; set; } = [];

    // What the current user may do (decided by IssuePermissions).
    public bool IsReporter { get; set; }
    public bool CanEdit { get; set; }
    public bool CanCancel { get; set; }
}

public class CancelIssueViewModel
{
    [StringLength(500)]
    public string? Reason { get; set; }
}
