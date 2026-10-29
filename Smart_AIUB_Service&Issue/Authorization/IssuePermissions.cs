using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.Authorization;

/// <summary>Who is acting: their id, role and (for support staff) their team.</summary>
public record IssueActor(string UserId, bool IsAdmin, bool IsStaff, int? TeamId);

/// <summary>
/// Every "who may do what to this issue" rule in one place. Plain functions with no
/// database access, so they are easy to read and to unit-test. Used by the
/// authorization handler, by the workflow service and to show/hide buttons.
///
/// Lifecycle (plan section 5):
///   Submitted → Assigned → InProgress ⇄ OnHold → Resolved → Closed
///   Submitted → Rejected | Cancelled;  Resolved → Assigned (reopen)
/// </summary>
public static class IssuePermissions
{
    // ---------- Who is this person to the issue? ----------

    public static bool IsReporter(Issue issue, IssueActor actor) => issue.ReporterId == actor.UserId;

    /// <summary>Support staff whose team the issue is assigned to.</summary>
    public static bool IsTeamMember(Issue issue, IssueActor actor) =>
        actor.IsStaff && actor.TeamId is not null && issue.AssignedTeamId == actor.TeamId;

    /// <summary>The technician the issue is assigned to.</summary>
    public static bool IsAssignedStaff(Issue issue, IssueActor actor) =>
        IsTeamMember(issue, actor) && issue.AssignedStaffId == actor.UserId;

    // ---------- Viewing ----------

    /// <summary>Full details: the reporter, the assigned team's staff, or an admin.</summary>
    public static bool CanView(Issue issue, IssueActor actor) =>
        actor.IsAdmin || IsReporter(issue, actor) || IsTeamMember(issue, actor);

    /// <summary>Internal (staff-only) comments.</summary>
    public static bool CanSeeInternal(Issue issue, IssueActor actor) =>
        actor.IsAdmin || IsTeamMember(issue, actor);

    // ---------- Reporter ----------

    /// <summary>Edit only before an admin has reviewed it.</summary>
    public static bool CanEdit(Issue issue, IssueActor actor) =>
        issue.Status == IssueStatus.Submitted && (IsReporter(issue, actor) || actor.IsAdmin);

    public static bool CanCancel(Issue issue, IssueActor actor) => CanEdit(issue, actor);

    /// <summary>Resolved → Closed: the reporter confirms the fix (or an admin closes it).</summary>
    public static bool CanClose(Issue issue, IssueActor actor) =>
        issue.Status == IssueStatus.Resolved && (IsReporter(issue, actor) || actor.IsAdmin);

    /// <summary>Resolved → Assigned: only the reporter, and only within the reopen window.</summary>
    public static bool CanReopen(Issue issue, IssueActor actor, DateTime nowUtc, int windowDays) =>
        issue.Status == IssueStatus.Resolved &&
        IsReporter(issue, actor) &&
        issue.ResolvedAt is not null &&
        nowUtc <= issue.ResolvedAt.Value.AddDays(windowDays);

    // ---------- Admin review ----------

    /// <summary>Submitted → Assigned, or reassign while the work is not finished.</summary>
    public static bool CanAssign(Issue issue, IssueActor actor) =>
        actor.IsAdmin && issue.Status is IssueStatus.Submitted or IssueStatus.Assigned
                                       or IssueStatus.InProgress or IssueStatus.OnHold;

    /// <summary>Submitted → Rejected (duplicate, not a campus problem, ...).</summary>
    public static bool CanReject(Issue issue, IssueActor actor) =>
        actor.IsAdmin && issue.Status == IssueStatus.Submitted;

    // ---------- Support staff work ----------

    /// <summary>A team member picks an unassigned issue from the team queue.</summary>
    public static bool CanTake(Issue issue, IssueActor actor) =>
        issue.Status == IssueStatus.Assigned && issue.AssignedStaffId is null && IsTeamMember(issue, actor);

    /// <summary>Assigned → InProgress, by the assigned technician (or any team member if nobody is assigned yet).</summary>
    public static bool CanStart(Issue issue, IssueActor actor) =>
        issue.Status == IssueStatus.Assigned &&
        (actor.IsAdmin || IsAssignedStaff(issue, actor) || (issue.AssignedStaffId is null && IsTeamMember(issue, actor)));

    /// <summary>InProgress → OnHold (reason required).</summary>
    public static bool CanHold(Issue issue, IssueActor actor) =>
        issue.Status == IssueStatus.InProgress && (actor.IsAdmin || IsAssignedStaff(issue, actor));

    /// <summary>OnHold → InProgress.</summary>
    public static bool CanResume(Issue issue, IssueActor actor) =>
        issue.Status == IssueStatus.OnHold && (actor.IsAdmin || IsAssignedStaff(issue, actor));

    /// <summary>InProgress → Resolved (resolution note required).</summary>
    public static bool CanResolve(Issue issue, IssueActor actor) =>
        issue.Status == IssueStatus.InProgress && (actor.IsAdmin || IsAssignedStaff(issue, actor));

    // ---------- Comments ----------

    /// <summary>Anyone who can see the issue may comment until it is finished.</summary>
    public static bool CanComment(Issue issue, IssueActor actor) =>
        CanView(issue, actor) && !issue.Status.IsFinal();
}
