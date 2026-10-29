using Microsoft.AspNetCore.Mvc.Rendering;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IDepartmentService
{
    Task<PagedResult<Department>> GetPageAsync(string? search, string? status, int page);
    Task<DepartmentFormViewModel?> GetFormAsync(int id);

    /// <summary>Creates (Id = 0) or updates a department. Code and name must be unique.</summary>
    Task<ServiceResult> SaveAsync(DepartmentFormViewModel model);

    Task<ServiceResult> SetActiveAsync(int id, bool isActive);

    /// <summary>Active departments, plus <paramref name="includeId"/> even if inactive (for edit forms).</summary>
    Task<List<SelectListItem>> GetOptionsAsync(int? includeId = null);
}
