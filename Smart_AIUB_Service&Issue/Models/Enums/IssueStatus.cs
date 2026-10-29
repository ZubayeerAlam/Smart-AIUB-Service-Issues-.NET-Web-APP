namespace Smart_AIUB_Service_Issue.Models.Enums;

/// <summary>
/// Lifecycle of an issue. Allowed moves between these values are enforced by the
/// issue workflow service, never by setting the property directly.
/// Closed, Rejected and Cancelled are final states.
/// </summary>
public enum IssueStatus
{
    Submitted,
    Assigned,
    InProgress,
    OnHold,
    Resolved,
    Closed,
    Rejected,
    Cancelled
}
