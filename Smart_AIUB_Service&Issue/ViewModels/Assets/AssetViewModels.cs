using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.ViewModels.Assets;

// ---------- Asset types ----------

public record AssetTypeListItem(int Id, string Name, int AssetCount, bool IsActive);

/// <summary>Add/Edit form. Id = 0 means "new".</summary>
public class AssetTypeFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
}

// ---------- Assets ----------

public record AssetListItem(
    int Id, string AssetTag, string Name, string TypeName, string BuildingName, string RoomNumber,
    AssetStatus Status, DateTime? LastMaintenanceAt);

public class AssetIndexViewModel
{
    public string? Search { get; set; }
    public int? TypeId { get; set; }
    public int? BuildingId { get; set; }
    public AssetStatus? Status { get; set; }
    public PagedResult<AssetListItem> Results { get; set; } = null!;
    public IEnumerable<SelectListItem> TypeOptions { get; set; } = [];
    public IEnumerable<SelectListItem> BuildingOptions { get; set; } = [];
}

/// <summary>Add/Edit form (Admin). Id = 0 means "new".</summary>
public class AssetFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    [RegularExpression(@"^[A-Za-z0-9-]+$", ErrorMessage = "Use letters, digits or '-' only, e.g. PRJ-D301.")]
    [Display(Name = "Asset tag")]
    public string AssetTag { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a type.")]
    [Display(Name = "Type")]
    public int? AssetTypeId { get; set; }

    [Required(ErrorMessage = "Please select a building.")]
    [Display(Name = "Building")]
    public int? BuildingId { get; set; }

    [Required(ErrorMessage = "Please select a room.")]
    [Display(Name = "Floor & room")]
    public int? RoomId { get; set; }

    [StringLength(100)]
    [Display(Name = "Serial number")]
    public string? SerialNumber { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Active;

    [DataType(DataType.Date)]
    [Display(Name = "Purchase date")]
    public DateOnly? PurchaseDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Warranty until")]
    public DateOnly? WarrantyExpiry { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [ValidateNever] public IEnumerable<SelectListItem> TypeOptions { get; set; } = [];
    [ValidateNever] public IEnumerable<SelectListItem> BuildingOptions { get; set; } = [];
    [ValidateNever] public IEnumerable<SelectListItem> RoomOptions { get; set; } = [];

    /// <summary>Cross-field check: runs together with the attribute checks.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PurchaseDate is not null && WarrantyExpiry is not null && WarrantyExpiry < PurchaseDate)
        {
            yield return new ValidationResult("Warranty cannot end before the purchase date.", [nameof(WarrantyExpiry)]);
        }
    }
}

public record MaintenanceRecordItem(
    int Id, MaintenanceType Type, string Description, DateTime PerformedAt, string PerformedByName,
    decimal? Cost, int? IssueId);

public record LinkedIssueItem(int Id, string Title, IssueStatus Status, DateTime CreatedAt)
{
    public string TicketNumber => IssueDisplay.TicketNumber(Id);
}

public class AssetDetailsViewModel
{
    public int Id { get; set; }
    public string AssetTag { get; set; } = "";
    public string Name { get; set; } = "";
    public string TypeName { get; set; } = "";
    public string BuildingName { get; set; } = "";
    public int Floor { get; set; }
    public string RoomNumber { get; set; } = "";
    public string? SerialNumber { get; set; }
    public AssetStatus Status { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? WarrantyExpiry { get; set; }
    public string? Notes { get; set; }
    public List<MaintenanceRecordItem> Maintenance { get; set; } = [];
    public List<LinkedIssueItem> Issues { get; set; } = [];

    public decimal TotalCost => Maintenance.Sum(m => m.Cost ?? 0);
    public bool UnderWarranty => WarrantyExpiry is not null && WarrantyExpiry >= DateOnly.FromDateTime(DateTime.UtcNow.ToDhaka());
}

// ---------- Maintenance ----------

/// <summary>"Log maintenance" form (Support Staff / Admin).</summary>
public class MaintenanceFormViewModel
{
    public int AssetId { get; set; }

    [Display(Name = "Related issue (optional)")]
    public int? IssueId { get; set; }

    [Required]
    public MaintenanceType Type { get; set; } = MaintenanceType.Corrective;

    [Required, StringLength(2000, MinimumLength = 5)]
    [Display(Name = "Work done")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Bangladesh time, as typed in the form. Converted to UTC when saved.</summary>
    [Required]
    [Display(Name = "Date & time")]
    public DateTime? PerformedAtLocal { get; set; }

    [Range(0, 99_999_999.99, ErrorMessage = "Cost must be between 0 and 99,999,999.99.")]
    [Display(Name = "Cost in BDT (optional)")]
    public decimal? Cost { get; set; }

    [Display(Name = "Asset status after this work")]
    public AssetStatus AssetStatus { get; set; }

    [ValidateNever] public string AssetLabel { get; set; } = "";
    [ValidateNever] public IEnumerable<SelectListItem> IssueOptions { get; set; } = [];
}

/// <summary>Staff/admin choose which asset of the room an issue is about.</summary>
public class LinkAssetViewModel
{
    public int? AssetId { get; set; }
}
