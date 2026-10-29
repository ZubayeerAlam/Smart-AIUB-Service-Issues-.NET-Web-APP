using Microsoft.AspNetCore.Mvc.Rendering;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IBuildingService
{
    Task<PagedResult<BuildingListItem>> GetPageAsync(string? search, string? status, int page);
    Task<BuildingFormViewModel?> GetFormAsync(int id);

    /// <summary>Creates (Id = 0) or updates a building. Code and name must be unique.</summary>
    Task<ServiceResult> SaveAsync(BuildingFormViewModel model);

    Task<ServiceResult> SetActiveAsync(int id, bool isActive);

    /// <summary>Active buildings, plus <paramref name="includeId"/> even if inactive (for edit forms).</summary>
    Task<List<SelectListItem>> GetOptionsAsync(int? includeId = null);
}
