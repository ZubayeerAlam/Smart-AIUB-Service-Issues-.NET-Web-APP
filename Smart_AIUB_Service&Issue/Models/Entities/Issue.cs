using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>A problem reported by a user. The core table of the system.</summary>
public class Issue
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public IssueCategory Category { get; set; } = null!;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public string ReporterId { get; set; } = string.Empty;
    public ApplicationUser Reporter { get; set; } = null!;

    public IssueStatus Status { get; set; } = IssueStatus.Submitted;

    /// <summary>Null until an Admin reviews the issue.</summary>
    public IssuePriority? Priority { get; set; }

    public int? AssignedTeamId { get; set; }
    public SupportTeam? AssignedTeam { get; set; }

    public string? AssignedStaffId { get; set; }
    public ApplicationUser? AssignedStaff { get; set; }

    /// <summary>Optional asset this issue is about. Linked by staff, not the reporter.</summary>
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }

    /// <summary>Required when the issue moves to Resolved.</summary>
    public string? ResolutionNote { get; set; }

    public int ReopenCount { get; set; }

    // All times are stored in UTC.
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Concurrency token. SQL Server changes it on every update, so if two admins
    /// edit the same issue at once, the second save fails instead of silently
    /// overwriting the first.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<IssueComment> Comments { get; set; } = new List<IssueComment>();
    public ICollection<IssueStatusHistory> StatusHistory { get; set; } = new List<IssueStatusHistory>();
}
