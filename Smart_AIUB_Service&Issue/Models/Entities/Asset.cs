using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>A physical item installed in a room, e.g. projector PRJ-D301.</summary>
public class Asset
{
    public int Id { get; set; }

    /// <summary>Unique inventory tag printed on the item.</summary>
    public string AssetTag { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int AssetTypeId { get; set; }
    public AssetType AssetType { get; set; } = null!;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public string? SerialNumber { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Active;

    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? WarrantyExpiry { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}
