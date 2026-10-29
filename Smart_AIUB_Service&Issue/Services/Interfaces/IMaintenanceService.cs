using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.ViewModels.Assets;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IMaintenanceService
{
    /// <summary>New form for an asset, optionally pre-linked to an issue. Null if the asset doesn't exist.</summary>
    Task<MaintenanceFormViewModel?> NewFormAsync(int assetId, int? issueId);

    /// <summary>Fills the asset label and the "related issue" dropdown (issues linked to this asset).</summary>
    Task FillFormOptionsAsync(MaintenanceFormViewModel model);

    /// <summary>
    /// Saves a maintenance record and the asset's new status together. A related issue must be
    /// linked to the same asset; the date cannot be in the future.
    /// </summary>
    Task<ServiceResult> CreateAsync(MaintenanceFormViewModel model, IssueActor actor);
}
