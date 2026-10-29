using System.ComponentModel.DataAnnotations;

namespace Smart_AIUB_Service_Issue.ViewModels.Admin;

public record BuildingListItem(int Id, string Code, string Name, int RoomCount, bool IsActive);

/// <summary>Add/Edit form. Id = 0 means "new".</summary>
public class BuildingFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    [RegularExpression(@"^[A-Za-z0-9-]+$", ErrorMessage = "Use letters, digits or '-' only.")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
}
