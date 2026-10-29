using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

/// <summary>
/// The only place where an issue's Status changes. Every change is checked against
/// the allowed transitions and writes one IssueStatusHistory row.
/// (M4 has Cancel; M5 adds assign, start, resolve, close, reopen, ...)
/// </summary>
public interface IIssueWorkflowService
{
    /// <summary>Submitted → Cancelled, by the reporter or an admin.</summary>
    Task<ServiceResult> CancelAsync(int issueId, string? reason, IssueActor actor);
}
