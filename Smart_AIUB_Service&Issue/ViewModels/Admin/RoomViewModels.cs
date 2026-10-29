using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smart_AIUB_Service_Issue.ViewModels.Admin;

public record RoomListItem(
    int Id, string BuildingCode, string BuildingName, int Floor, string RoomNumber, string? Name, bool IsActive);

/// <summary>Room list adds a building filter on top of search + status.</summary>
public class RoomIndexViewModel : ListPageViewModel<RoomListItem>
{
    public int? BuildingId { get; set; }
    public IEnumerable<SelectListItem> BuildingOptions { get; set; } = [];
}

/// <summary>Add/Edit form. Id = 0 means "new".</summary>
public class RoomFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please select a building.")]
    [Display(Name = "Building")]
    public int? BuildingId { get; set; }

    [Range(0, 50, ErrorMessage = "Floor must be between 0 (ground) and 50.")]
    public int Floor { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Room number")]
    public string RoomNumber { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Room name (optional)")]
    public string? Name { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> BuildingOptions { get; set; } = [];
}
