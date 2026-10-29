namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>Type of problem, e.g. Projector, Wi-Fi/Network, Electrical.</summary>
public class IssueCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Pre-selected team on the admin review screen.</summary>
    public int? DefaultSupportTeamId { get; set; }
    public SupportTeam? DefaultSupportTeam { get; set; }

    public bool IsActive { get; set; } = true;
}
