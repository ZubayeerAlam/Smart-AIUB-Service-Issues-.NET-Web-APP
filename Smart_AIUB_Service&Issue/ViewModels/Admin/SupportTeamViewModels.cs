using System.ComponentModel.DataAnnotations;

namespace Smart_AIUB_Service_Issue.ViewModels.Admin;

public record SupportTeamListItem(int Id, string Name, string? Description, int MemberCount, bool IsActive);

/// <summary>Add/Edit form. Id = 0 means "new".</summary>
public class SupportTeamFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}
