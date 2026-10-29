using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.Common;

/// <summary>Small helpers for showing issues consistently on every page.</summary>
public static class IssueDisplay
{
    /// <summary>Statuses where nothing more can happen to the issue.</summary>
    public static readonly IssueStatus[] FinalStatuses = [IssueStatus.Closed, IssueStatus.Rejected, IssueStatus.Cancelled];

    /// <summary>Ticket number derived from the Id, e.g. 123 => "ISS-000123".</summary>
    public static string TicketNumber(int id) => $"ISS-{id:000000}";

    public static bool IsFinal(this IssueStatus status) => FinalStatuses.Contains(status);

    public static string DisplayName(this IssueStatus status) => status switch
    {
        IssueStatus.InProgress => "In progress",
        IssueStatus.OnHold => "On hold",
        _ => status.ToString()
    };

    /// <summary>CSS class for the coloured status badge.</summary>
    public static string BadgeClass(this IssueStatus status) => status switch
    {
        IssueStatus.Submitted => "sc-st-submitted",
        IssueStatus.Assigned => "sc-st-assigned",
        IssueStatus.InProgress => "sc-st-progress",
        IssueStatus.OnHold => "sc-st-hold",
        IssueStatus.Resolved => "sc-st-resolved",
        IssueStatus.Closed => "sc-st-closed",
        _ => "sc-st-stopped" // Rejected, Cancelled
    };

    public static string FloorName(int floor) => floor == 0 ? "Ground floor" : $"Floor {floor}";
}
