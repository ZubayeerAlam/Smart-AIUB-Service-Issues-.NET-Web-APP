namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>Campus building such as D-Building, C-Building or Annex 1.</summary>
public class Building
{
    public int Id { get; set; }

    /// <summary>Short unique code, e.g. "D", "C", "ANX-1".</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
