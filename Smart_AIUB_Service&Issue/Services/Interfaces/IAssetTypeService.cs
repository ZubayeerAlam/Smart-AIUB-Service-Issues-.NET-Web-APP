using Microsoft.AspNetCore.Mvc.Rendering;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.ViewModels.Assets;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IAssetTypeService
{
    Task<PagedResult<AssetTypeListItem>> GetPageAsync(string? search, string? status, int page);
    Task<AssetTypeFormViewModel?> GetFormAsync(int id);

    /// <summary>Creates (Id = 0) or updates an asset type. Name must be unique.</summary>
    Task<ServiceResult> SaveAsync(AssetTypeFormViewModel model);

    Task<ServiceResult> SetActiveAsync(int id, bool isActive);

    /// <summary>Active types, plus <paramref name="includeId"/> even if inactive (for edit forms).</summary>
    Task<List<SelectListItem>> GetOptionsAsync(int? includeId = null);
}
