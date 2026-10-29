using Microsoft.AspNetCore.Mvc.Rendering;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface ISupportTeamService
{
    Task<PagedResult<SupportTeamListItem>> GetPageAsync(string? search, string? status, int page);
    Task<SupportTeamFormViewModel?> GetFormAsync(int id);

    /// <summary>Creates (Id = 0) or updates a team. Name must be unique.</summary>
    Task<ServiceResult> SaveAsync(SupportTeamFormViewModel model);

    Task<ServiceResult> SetActiveAsync(int id, bool isActive);

    /// <summary>Active teams, plus <paramref name="includeId"/> even if inactive (for edit forms).</summary>
    Task<List<SelectListItem>> GetOptionsAsync(int? includeId = null);
}
