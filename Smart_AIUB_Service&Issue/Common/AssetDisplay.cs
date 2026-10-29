using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.Common;

/// <summary>Display helpers for assets and maintenance records.</summary>
public static class AssetDisplay
{
    public static string DisplayName(this AssetStatus status) => status switch
    {
        AssetStatus.UnderMaintenance => "Under maintenance",
        AssetStatus.OutOfService => "Out of service",
        _ => status.ToString()
    };

    public static string BadgeClass(this AssetStatus status) => status switch
    {
        AssetStatus.Active => "sc-badge-success",
        AssetStatus.UnderMaintenance => "sc-badge-warning",
        AssetStatus.OutOfService => "sc-st-stopped",
        _ => "sc-badge-muted" // Retired
    };

    /// <summary>"৳ 1,250.00" style amount.</summary>
    public static string Money(decimal amount) => "৳ " + amount.ToString("N2");
}
