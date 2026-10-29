using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>
/// One row per status change (or reassignment). Rows are only ever inserted,
/// never edited, so this is the issue's audit trail.
/// </summary>
public class IssueStatusHistory
{
    public int Id { get; set; }

    public int IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    /// <summary>Null for the very first entry (issue created).</summary>
    public IssueStatus? FromStatus { get; set; }
    public IssueStatus ToStatus { get; set; }

    public string ChangedById { get; set; } = string.Empty;
    public ApplicationUser ChangedBy { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    /// <summary>Reason or remark, e.g. rejection reason or reopen reason.</summary>
    public string? Note { get; set; }
}
