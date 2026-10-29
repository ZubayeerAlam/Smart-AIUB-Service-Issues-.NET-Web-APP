using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IIssueCategoryService
{
    Task<PagedResult<IssueCategoryListItem>> GetPageAsync(string? search, string? status, int page);
    Task<IssueCategoryFormViewModel?> GetFormAsync(int id);

    /// <summary>Creates (Id = 0) or updates a category. Name must be unique; the default team must be active.</summary>
    Task<ServiceResult> SaveAsync(IssueCategoryFormViewModel model);

    Task<ServiceResult> SetActiveAsync(int id, bool isActive);
}
