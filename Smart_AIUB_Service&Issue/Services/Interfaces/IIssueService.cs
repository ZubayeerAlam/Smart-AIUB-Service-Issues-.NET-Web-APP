using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IIssueService
{
    /// <summary>Fills the Category/Building/Room dropdowns of the report form.</summary>
    Task FillFormOptionsAsync(IssueFormViewModel model);

    /// <summary>Active rooms of an active building, for the cascading dropdown.</summary>
    Task<List<RoomOption>> GetRoomOptionsAsync(int buildingId);

    /// <summary>Creates a new issue in status Submitted and records the first history entry.</summary>
    Task<ServiceResult<int>> ReportAsync(IssueFormViewModel model, string reporterId);

    /// <summary>The issue entity alone (no related data), for authorization checks.</summary>
    Task<Issue?> FindAsync(int id);

    Task<IssueFormViewModel?> GetEditFormAsync(int id);

    /// <summary>Updates an issue. Only allowed while Submitted, by its reporter or an admin.</summary>
    Task<ServiceResult> UpdateAsync(IssueFormViewModel model, IssueActor actor);

    /// <summary>Issues reported by <paramref name="userId"/>.</summary>
    Task<PagedResult<IssueListItem>> GetMyIssuesAsync(string userId, string? search, IssueStatus? status, int page);

    /// <summary>Everyone's issues that are still open. No reporter names.</summary>
    Task<PagedResult<IssueListItem>> GetOpenIssuesAsync(string? search, IssueStatus? status, int page);

    /// <summary>Full details for the detail page. Call only after the View check passed.</summary>
    Task<IssueDetailsViewModel?> GetDetailsAsync(int id, IssueActor actor);
}
