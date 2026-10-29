namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>Kind of asset, e.g. Projector, Desktop PC, Router, Air Conditioner.</summary>
public class AssetType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
