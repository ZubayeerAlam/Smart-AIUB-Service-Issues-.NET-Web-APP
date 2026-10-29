using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IIssueService
{
    // Report form
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

    // Lists
    /// <summary>Issues reported by <paramref name="userId"/>.</summary>
    Task<PagedResult<IssueListItem>> GetMyIssuesAsync(string userId, IssueListFilter filter, int page);

    /// <summary>Everyone's issues that are still open. No reporter names.</summary>
    Task<PagedResult<IssueListItem>> GetOpenIssuesAsync(IssueListFilter filter, int page);

    /// <summary>Category and building dropdowns for the list filters.</summary>
    Task FillListOptionsAsync(IssueListViewModel model);

    /// <summary>Admin: Submitted issues waiting for review, oldest first.</summary>
    Task<PagedResult<IssueListItem>> GetReviewQueueAsync(string? search, int page);

    /// <summary>Admin: every issue with filters.</summary>
    Task<PagedResult<IssueListItem>> GetAllIssuesAsync(IssueListFilter filter, int page);

    /// <summary>Support staff: issues of their own team ("mine", "unassigned" or all).</summary>
    Task<PagedResult<IssueListItem>> GetTeamIssuesAsync(IssueActor actor, string? scope, IssueListFilter filter, int page);

    // Details
    /// <summary>Full details, comments and allowed actions. Call only after the View check passed.</summary>
    Task<IssueDetailsViewModel?> GetDetailsAsync(int id, IssueActor actor);

    // Assets
    /// <summary>Links the issue to an asset in the same room (or unlinks with null). Staff of the team or admin.</summary>
    Task<ServiceResult> LinkAssetAsync(int issueId, int? assetId, IssueActor actor);

    // Comments
    /// <summary>Adds a comment. Internal notes are allowed for the assigned team's staff and admins only.</summary>
    Task<ServiceResult> AddCommentAsync(int issueId, AddCommentViewModel model, IssueActor actor);
}
