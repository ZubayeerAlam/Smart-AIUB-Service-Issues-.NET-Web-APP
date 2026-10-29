using Microsoft.AspNetCore.Mvc.Rendering;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.ViewModels.Assets;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IAssetService
{
    Task<PagedResult<AssetListItem>> GetPageAsync(string? search, int? typeId, int? buildingId, AssetStatus? status, int page);

    Task<AssetFormViewModel?> GetFormAsync(int id);

    /// <summary>Fills the Type / Building / Room dropdowns.</summary>
    Task FillFormOptionsAsync(AssetFormViewModel model);

    /// <summary>
    /// Creates (Id = 0) or updates an asset. The tag must be unique, the room must belong to the
    /// chosen building, and new choices of type/room must be active. Returns the asset id.
    /// </summary>
    Task<ServiceResult<int>> SaveAsync(AssetFormViewModel model);

    Task<AssetDetailsViewModel?> GetDetailsAsync(int id);

    /// <summary>Assets in one room (for linking an issue to an asset). Retired assets are left out.</summary>
    Task<List<SelectListItem>> GetRoomAssetOptionsAsync(int roomId);
}
