using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

/// <summary>
/// The only place where an issue's Status changes. Every method checks the allowed
/// transition and the actor (IssuePermissions), validates required input, and writes
/// exactly one IssueStatusHistory row in the same save.
/// </summary>
public interface IIssueWorkflowService
{
    // Admin review
    /// <summary>Submitted → Assigned, or reassign (Assigned/InProgress/OnHold → Assigned). Priority + active team required.</summary>
    Task<ServiceResult> AssignAsync(int issueId, IssuePriority? priority, int? teamId, string? staffId, string? note, IssueActor actor);

    /// <summary>Submitted → Rejected. Reason required.</summary>
    Task<ServiceResult> RejectAsync(int issueId, string? reason, IssueActor actor);

    // Support staff
    /// <summary>Team member takes an unassigned issue (status stays Assigned).</summary>
    Task<ServiceResult> TakeAsync(int issueId, IssueActor actor);

    /// <summary>Assigned → InProgress.</summary>
    Task<ServiceResult> StartAsync(int issueId, IssueActor actor);

    /// <summary>InProgress → OnHold. Reason required.</summary>
    Task<ServiceResult> HoldAsync(int issueId, string? reason, IssueActor actor);

    /// <summary>OnHold → InProgress.</summary>
    Task<ServiceResult> ResumeAsync(int issueId, IssueActor actor);

    /// <summary>InProgress → Resolved. Resolution note required.</summary>
    Task<ServiceResult> ResolveAsync(int issueId, string? resolutionNote, IssueActor actor);

    // Reporter
    /// <summary>Submitted → Cancelled, by the reporter or an admin.</summary>
    Task<ServiceResult> CancelAsync(int issueId, string? reason, IssueActor actor);

    /// <summary>Resolved → Closed (reporter confirms the fix, or an admin closes it).</summary>
    Task<ServiceResult> CloseAsync(int issueId, IssueActor actor);

    /// <summary>Resolved → Assigned, by the reporter within the reopen window. Reason required.</summary>
    Task<ServiceResult> ReopenAsync(int issueId, string? reason, IssueActor actor);
}
