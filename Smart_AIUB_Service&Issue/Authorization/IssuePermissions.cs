using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.Authorization;

/// <summary>Who is acting: their id, role and (for support staff) their team.</summary>
public record IssueActor(string UserId, bool IsAdmin, bool IsStaff, int? TeamId);

/// <summary>
/// Every "who may do what to this issue" rule in one place. Plain functions with no
/// database access, so they are easy to read and to unit-test. Used by the
/// authorization handler, by the services and to show/hide buttons.
/// </summary>
public static class IssuePermissions
{
    public static bool IsReporter(Issue issue, IssueActor actor) => issue.ReporterId == actor.UserId;

    /// <summary>Support staff whose team the issue is assigned to.</summary>
    public static bool IsTeamMember(Issue issue, IssueActor actor) =>
        actor.IsStaff && actor.TeamId is not null && issue.AssignedTeamId == actor.TeamId;

    /// <summary>Full details: the reporter, the assigned team's staff, or an admin.</summary>
    public static bool CanView(Issue issue, IssueActor actor) =>
        actor.IsAdmin || IsReporter(issue, actor) || IsTeamMember(issue, actor);

    /// <summary>Edit only before an admin has reviewed it.</summary>
    public static bool CanEdit(Issue issue, IssueActor actor) =>
        issue.Status == IssueStatus.Submitted && (IsReporter(issue, actor) || actor.IsAdmin);

    public static bool CanCancel(Issue issue, IssueActor actor) => CanEdit(issue, actor);
}
