using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IRoomService
{
    Task<PagedResult<RoomListItem>> GetPageAsync(string? search, string? status, int? buildingId, int page);
    Task<RoomFormViewModel?> GetFormAsync(int id);

    /// <summary>
    /// Creates (Id = 0) or updates a room. The room number must be unique inside its building,
    /// and new rooms can only be added to an active building.
    /// </summary>
    Task<ServiceResult> SaveAsync(RoomFormViewModel model);

    Task<ServiceResult> SetActiveAsync(int id, bool isActive);
}
