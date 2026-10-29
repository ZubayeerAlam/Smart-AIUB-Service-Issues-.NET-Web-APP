namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>Academic department such as CSE, EEE or BBA. Used on user profiles.</summary>
public class Department
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
