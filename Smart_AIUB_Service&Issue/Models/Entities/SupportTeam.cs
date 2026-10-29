namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>Group of support staff, e.g. IT Support, Electrical, Furniture.</summary>
public class SupportTeam
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
}
