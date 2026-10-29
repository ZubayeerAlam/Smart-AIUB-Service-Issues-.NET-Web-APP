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

/// <summary>
/// One row in any issue list. ReporterName and AssignedTo are only filled on
/// staff/admin lists (null on the reporter's and the public list).
/// </summary>
public record IssueListItem(
    int Id,
    string Title,
    string CategoryName,
    string BuildingName,
    string RoomNumber,
    IssueStatus Status,
    IssuePriority? Priority,
    DateTime CreatedAt,
    string? ReporterName,
    string? AssignedTo)
{
    public string TicketNumber => IssueDisplay.TicketNumber(Id);
}

/// <summary>Filter values shared by every issue list (unused ones stay null).</summary>
public class IssueListFilter
{
    public string? Search { get; set; }
    public IssueStatus? Status { get; set; }
    public IssuePriority? Priority { get; set; }
    public int? TeamId { get; set; }
}

public class IssueListViewModel : IssueListFilter
{
    public PagedResult<IssueListItem> Results { get; set; } = null!;

    /// <summary>Team dropdown on the admin "All issues" list.</summary>
    public IEnumerable<SelectListItem> TeamOptions { get; set; } = [];

    /// <summary>Staff team queue: "mine", "unassigned" or "" (whole team).</summary>
    public string? Scope { get; set; }
}

/// <summary>Values for the staff team queue "Scope" filter.</summary>
public static class TeamQueueScope
{
    public const string Mine = "mine";
    public const string Unassigned = "unassigned";
}

public record IssueHistoryItem(IssueStatus? FromStatus, IssueStatus ToStatus, string ChangedByName, DateTime ChangedAt, string? Note);

public record IssueCommentItem(string AuthorName, string? AuthorRole, string Body, bool IsInternal, DateTime CreatedAt);

/// <summary>A technician choice in the assign form; TeamId lets the page filter by team.</summary>
public record StaffChoice(string Id, string Name, int TeamId);

/// <summary>Admin "Assign / reassign" form on the details page.</summary>
public class AssignIssueViewModel
{
    [Required(ErrorMessage = "Please choose a priority.")]
    public IssuePriority? Priority { get; set; }

    [Required(ErrorMessage = "Please choose a support team.")]
    [Display(Name = "Support team")]
    public int? TeamId { get; set; }

    [Display(Name = "Technician (optional)")]
    public string? StaffId { get; set; }

    [StringLength(1000)]
    [Display(Name = "Note (optional)")]
    public string? Note { get; set; }
}

/// <summary>Any action that needs a short text: reject, hold, resolve, reopen, cancel.</summary>
public class IssueNoteViewModel
{
    [StringLength(2000)]
    public string? Note { get; set; }
}

public class AddCommentViewModel
{
    [Required(ErrorMessage = "Please write a comment.")]
    [StringLength(2000)]
    public string Body { get; set; } = string.Empty;

    /// <summary>Staff/admin only; ignored (and refused) for reporters.</summary>
    public bool IsInternal { get; set; }
}

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
    public DateTime? ResolvedAt { get; set; }
    public int ReopenCount { get; set; }
    public List<IssueHistoryItem> History { get; set; } = [];
    public List<IssueCommentItem> Comments { get; set; } = [];

    // What the current user may do (decided by IssuePermissions).
    public bool IsReporter { get; set; }
    public bool CanEdit { get; set; }
    public bool CanCancel { get; set; }
    public bool CanAssign { get; set; }
    public bool CanReject { get; set; }
    public bool CanTake { get; set; }
    public bool CanStart { get; set; }
    public bool CanHold { get; set; }
    public bool CanResume { get; set; }
    public bool CanResolve { get; set; }
    public bool CanClose { get; set; }
    public bool CanReopen { get; set; }
    public bool CanComment { get; set; }
    public bool CanSeeInternal { get; set; }

    /// <summary>Last moment the reporter may reopen (UTC), when the issue is Resolved.</summary>
    public DateTime? ReopenUntil { get; set; }

    // Admin assign form (filled only when CanAssign).
    public AssignIssueViewModel Assign { get; set; } = new();
    public IEnumerable<SelectListItem> TeamOptions { get; set; } = [];
    public List<StaffChoice> StaffChoices { get; set; } = [];

    public bool HasAnyWorkAction => CanTake || CanStart || CanHold || CanResume || CanResolve;
}
