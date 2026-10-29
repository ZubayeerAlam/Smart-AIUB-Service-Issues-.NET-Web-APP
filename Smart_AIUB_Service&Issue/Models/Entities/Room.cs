namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>A specific location inside a building (floor + room number).</summary>
public class Room
{
    public int Id { get; set; }

    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;

    /// <summary>0 = ground floor.</summary>
    public int Floor { get; set; }

    /// <summary>Unique within its building, e.g. "301" or "Lab-2".</summary>
    public string RoomNumber { get; set; } = string.Empty;

    /// <summary>Optional friendly name, e.g. "Computer Lab 2".</summary>
    public string? Name { get; set; }

    public bool IsActive { get; set; } = true;
}
