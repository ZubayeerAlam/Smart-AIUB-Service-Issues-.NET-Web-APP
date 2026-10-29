using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>Work done on an asset, optionally because of a reported issue.</summary>
public class MaintenanceRecord
{
    public int Id { get; set; }

    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public int? IssueId { get; set; }
    public Issue? Issue { get; set; }

    public string PerformedById { get; set; } = string.Empty;
    public ApplicationUser PerformedBy { get; set; } = null!;

    public MaintenanceType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>When the work was done (UTC).</summary>
    public DateTime PerformedAt { get; set; }

    /// <summary>Optional cost in BDT.</summary>
    public decimal? Cost { get; set; }

    public DateTime CreatedAt { get; set; }
}
