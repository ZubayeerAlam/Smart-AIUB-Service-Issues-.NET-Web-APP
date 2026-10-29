using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.Services;

public class IssueService : IIssueService
{
    private readonly ApplicationDbContext _db;
    private readonly IssueOptions _options;

    public IssueService(ApplicationDbContext db, IOptions<IssueOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    // ---------- Report form ----------

    public async Task FillFormOptionsAsync(IssueFormViewModel model)
    {
        model.CategoryOptions = await _db.IssueCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name == "Other").ThenBy(c => c.Name) // "Other" last
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();

        model.BuildingOptions = await _db.Buildings.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
            .ToListAsync();

        model.RoomOptions = model.BuildingId is null
            ? []
            : (await GetRoomOptionsAsync(model.BuildingId.Value)).Select(r => new SelectListItem(r.Text, r.Id.ToString()));
    }

    public Task<List<RoomOption>> GetRoomOptionsAsync(int buildingId) =>
        _db.Rooms.AsNoTracking()
            .Where(r => r.BuildingId == buildingId && r.IsActive && r.Building.IsActive)
            .OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber)
            .Select(r => new RoomOption(
                r.Id,
                (r.Floor == 0 ? "Ground floor" : "Floor " + r.Floor) + " · Room " + r.RoomNumber +
                (r.Name != null ? " · " + r.Name : "")))
            .ToListAsync();

    public async Task<ServiceResult<int>> ReportAsync(IssueFormViewModel model, string reporterId)
    {
        var error = await ValidateLocationAndCategoryAsync(model);
        if (error is not null)
        {
            return ServiceResult<int>.Fail(error.Error!, error.Field);
        }

        var now = DateTime.UtcNow;
        var issue = new Issue
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            CategoryId = model.CategoryId!.Value,
            RoomId = model.RoomId!.Value,
            ReporterId = reporterId,
            Status = IssueStatus.Submitted,
            CreatedAt = now,
            UpdatedAt = now
        };

        // The first timeline entry: "Submitted".
        issue.StatusHistory.Add(new IssueStatusHistory
        {
            FromStatus = null,
            ToStatus = IssueStatus.Submitted,
            ChangedById = reporterId,
            ChangedAt = now,
            Note = "Issue reported"
        });

        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();
        return ServiceResult<int>.Success(issue.Id);
    }

    public Task<Issue?> FindAsync(int id) =>
        _db.Issues.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);

    public Task<IssueFormViewModel?> GetEditFormAsync(int id) =>
        _db.Issues.AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new IssueFormViewModel
            {
                Id = i.Id,
                CategoryId = i.CategoryId,
                BuildingId = i.Room.BuildingId,
                RoomId = i.RoomId,
                Title = i.Title,
                Description = i.Description
            })
            .FirstOrDefaultAsync();

    public async Task<ServiceResult> UpdateAsync(IssueFormViewModel model, IssueActor actor)
    {
        var issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == model.Id);
        if (issue is null)
        {
            return ServiceResult.Fail("Issue not found.");
        }

        // Checked again here (not only in the controller), right before saving.
        if (!IssuePermissions.CanEdit(issue, actor))
        {
            return ServiceResult.Fail("This issue can no longer be edited because it has already been reviewed.");
        }

        var error = await ValidateLocationAndCategoryAsync(model);
        if (error is not null)
        {
            return error;
        }

        issue.Title = model.Title.Trim();
        issue.Description = model.Description.Trim();
        issue.CategoryId = model.CategoryId!.Value;
        issue.RoomId = model.RoomId!.Value;
        issue.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // RowVersion changed: someone (e.g. an admin) updated the issue at the same moment.
            return ServiceResult.Fail("This issue was changed by someone else just now. Please reload and try again.");
        }
        return ServiceResult.Success();
    }

    // ---------- Lists ----------

    public Task<PagedResult<IssueListItem>> GetMyIssuesAsync(string userId, string? search, IssueStatus? status, int page) =>
        ToListItems(Filter(_db.Issues.Where(i => i.ReporterId == userId), new IssueListFilter { Search = search, Status = status }),
                staffView: false)
            .ToPagedResultAsync(page);

    public Task<PagedResult<IssueListItem>> GetOpenIssuesAsync(string? search, IssueStatus? status, int page) =>
        ToListItems(Filter(OpenIssues(), new IssueListFilter { Search = search, Status = status }), staffView: false)
            .ToPagedResultAsync(page);

    public Task<PagedResult<IssueListItem>> GetReviewQueueAsync(string? search, int page) =>
        ToListItems(Filter(_db.Issues.Where(i => i.Status == IssueStatus.Submitted), new IssueListFilter { Search = search }),
                staffView: true, oldestFirst: true) // first come, first served
            .ToPagedResultAsync(page);

    public Task<PagedResult<IssueListItem>> GetAllIssuesAsync(IssueListFilter filter, int page) =>
        ToListItems(Filter(_db.Issues, filter), staffView: true).ToPagedResultAsync(page);

    public Task<PagedResult<IssueListItem>> GetTeamIssuesAsync(IssueActor actor, string? scope, IssueListFilter filter, int page)
    {
        var query = _db.Issues.Where(i => actor.TeamId != null && i.AssignedTeamId == actor.TeamId);

        query = scope switch
        {
            TeamQueueScope.Mine => query.Where(i => i.AssignedStaffId == actor.UserId),
            TeamQueueScope.Unassigned => query.Where(i => i.AssignedStaffId == null && i.Status == IssueStatus.Assigned),
            _ => query
        };

        // Without a status filter, finished work is hidden so the queue stays short.
        if (filter.Status is null)
        {
            query = query.Where(i =>
                i.Status != IssueStatus.Closed && i.Status != IssueStatus.Rejected && i.Status != IssueStatus.Cancelled);
        }

        return ToListItems(Filter(query, filter), staffView: true).ToPagedResultAsync(page);
    }

    // ---------- Details ----------

    public async Task<IssueDetailsViewModel?> GetDetailsAsync(int id, IssueActor actor)
    {
        var issue = await _db.Issues.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (issue is null)
        {
            return null;
        }

        var model = await _db.Issues.AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new IssueDetailsViewModel
            {
                Id = i.Id,
                Title = i.Title,
                Description = i.Description,
                CategoryName = i.Category.Name,
                BuildingName = i.Room.Building.Name,
                RoomNumber = i.Room.RoomNumber,
                Floor = i.Room.Floor,
                RoomName = i.Room.Name,
                Status = i.Status,
                Priority = i.Priority,
                ReporterName = i.Reporter.FullName,
                ReporterEmail = i.Reporter.Email ?? "",
                TeamName = i.AssignedTeam != null ? i.AssignedTeam.Name : null,
                StaffName = i.AssignedStaff != null ? i.AssignedStaff.FullName : null,
                ResolutionNote = i.ResolutionNote,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                ResolvedAt = i.ResolvedAt,
                ReopenCount = i.ReopenCount,
                AssetId = i.AssetId,
                AssetLabel = i.Asset != null ? i.Asset.AssetTag + " · " + i.Asset.Name : null
            })
            .FirstAsync();

        model.History = await _db.IssueStatusHistory.AsNoTracking()
            .Where(h => h.IssueId == id)
            .OrderBy(h => h.ChangedAt).ThenBy(h => h.Id)
            .Select(h => new IssueHistoryItem(h.FromStatus, h.ToStatus, h.ChangedBy.FullName, h.ChangedAt, h.Note))
            .ToListAsync();

        // Internal notes are removed in the query itself, so they never reach a reporter's page.
        var canSeeInternal = IssuePermissions.CanSeeInternal(issue, actor);
        model.Comments = await _db.IssueComments.AsNoTracking()
            .Where(c => c.IssueId == id && (canSeeInternal || !c.IsInternal))
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .Select(c => new IssueCommentItem(
                c.Author.FullName,
                c.AuthorId == issue.ReporterId
                    ? "Reporter"
                    : (from ur in _db.UserRoles
                       join r in _db.Roles on ur.RoleId equals r.Id
                       where ur.UserId == c.AuthorId
                       select r.Name).FirstOrDefault(),
                c.Body,
                c.IsInternal,
                c.CreatedAt))
            .ToListAsync();

        var now = DateTime.UtcNow;
        model.IsReporter = IssuePermissions.IsReporter(issue, actor);
        model.CanEdit = IssuePermissions.CanEdit(issue, actor);
        model.CanCancel = IssuePermissions.CanCancel(issue, actor);
        model.CanAssign = IssuePermissions.CanAssign(issue, actor);
        model.CanReject = IssuePermissions.CanReject(issue, actor);
        model.CanTake = IssuePermissions.CanTake(issue, actor);
        model.CanStart = IssuePermissions.CanStart(issue, actor);
        model.CanHold = IssuePermissions.CanHold(issue, actor);
        model.CanResume = IssuePermissions.CanResume(issue, actor);
        model.CanResolve = IssuePermissions.CanResolve(issue, actor);
        model.CanClose = IssuePermissions.CanClose(issue, actor);
        model.CanReopen = IssuePermissions.CanReopen(issue, actor, now, _options.ReopenWindowDays);
        model.CanComment = IssuePermissions.CanComment(issue, actor);
        model.CanSeeInternal = canSeeInternal;
        model.ReopenUntil = issue.ResolvedAt?.AddDays(_options.ReopenWindowDays);
        model.CanLinkAsset = IssuePermissions.CanLinkAsset(issue, actor);
        model.CanSeeAssets = actor.IsAdmin || actor.IsStaff;

        if (model.CanAssign)
        {
            await FillAssignOptionsAsync(model, issue);
        }
        if (model.CanLinkAsset)
        {
            model.RoomAssetOptions = await _db.Assets.AsNoTracking()
                .Where(a => a.RoomId == issue.RoomId && (a.Status != AssetStatus.Retired || a.Id == issue.AssetId))
                .OrderBy(a => a.AssetTag)
                .Select(a => new SelectListItem(a.AssetTag + " · " + a.Name, a.Id.ToString(), a.Id == issue.AssetId))
                .ToListAsync();
        }
        return model;
    }

    // ---------- Asset link ----------

    public async Task<ServiceResult> LinkAssetAsync(int issueId, int? assetId, IssueActor actor)
    {
        var issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == issueId);
        if (issue is null)
        {
            return ServiceResult.Fail("Issue not found.");
        }
        if (!IssuePermissions.CanLinkAsset(issue, actor))
        {
            return ServiceResult.Fail("Only the assigned support team or an admin can link an asset to this issue.");
        }

        string note;
        if (assetId is null)
        {
            note = "Asset link removed";
        }
        else
        {
            var asset = await _db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assetId);
            if (asset is null)
            {
                return ServiceResult.Fail("Asset not found.");
            }
            // Business rule: the asset must be in the room where the problem was reported.
            if (asset.RoomId != issue.RoomId)
            {
                return ServiceResult.Fail("The asset must be in the same room as the issue.");
            }
            note = $"Linked to asset {asset.AssetTag}";
        }

        issue.AssetId = assetId;
        issue.UpdatedAt = DateTime.UtcNow;

        // Recorded in the timeline (status unchanged) so everyone sees who linked what.
        _db.IssueStatusHistory.Add(new IssueStatusHistory
        {
            IssueId = issue.Id,
            FromStatus = issue.Status,
            ToStatus = issue.Status,
            ChangedById = actor.UserId,
            ChangedAt = issue.UpdatedAt,
            Note = note
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("This issue was changed by someone else just now. Please reload and try again.");
        }
        return ServiceResult.Success();
    }

    private async Task FillAssignOptionsAsync(IssueDetailsViewModel model, Issue issue)
    {
        // Pre-select the current team, or the category's default team for a new issue.
        var defaultTeamId = issue.AssignedTeamId ??
            await _db.IssueCategories.Where(c => c.Id == issue.CategoryId).Select(c => c.DefaultSupportTeamId).FirstOrDefaultAsync();

        model.Assign = new AssignIssueViewModel
        {
            Priority = issue.Priority,
            TeamId = defaultTeamId,
            StaffId = issue.AssignedStaffId
        };

        model.TeamOptions = await _db.SupportTeams.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem(t.Name, t.Id.ToString()))
            .ToListAsync();

        model.StaffChoices = await (
            from u in _db.Users.AsNoTracking()
            join ur in _db.UserRoles on u.Id equals ur.UserId
            join r in _db.Roles on ur.RoleId equals r.Id
            where r.Name == Roles.SupportStaff && u.IsActive && u.SupportTeamId != null && u.SupportTeam!.IsActive
            orderby u.FullName
            select new StaffChoice(u.Id, u.FullName, u.SupportTeamId!.Value)).ToListAsync();
    }

    // ---------- Comments ----------

    public async Task<ServiceResult> AddCommentAsync(int issueId, AddCommentViewModel model, IssueActor actor)
    {
        var issue = await _db.Issues.AsNoTracking().FirstOrDefaultAsync(i => i.Id == issueId);
        if (issue is null)
        {
            return ServiceResult.Fail("Issue not found.");
        }
        if (!IssuePermissions.CanComment(issue, actor))
        {
            return ServiceResult.Fail("Comments are closed for this issue.");
        }
        if (model.IsInternal && !IssuePermissions.CanSeeInternal(issue, actor))
        {
            return ServiceResult.Fail("Only support staff and admins can write internal notes.");
        }

        var body = model.Body.Trim();
        if (body.Length == 0)
        {
            return ServiceResult.Fail("Please write a comment.", nameof(model.Body));
        }

        _db.IssueComments.Add(new IssueComment
        {
            IssueId = issueId,
            AuthorId = actor.UserId,
            Body = body,
            IsInternal = model.IsInternal,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    // ---------- Helpers ----------

    /// <summary>
    /// Category must be active; the room must be active, in an active building, and
    /// belong to the chosen building (checked here because a posted form can be edited).
    /// </summary>
    private async Task<ServiceResult?> ValidateLocationAndCategoryAsync(IssueFormViewModel model)
    {
        if (!await _db.IssueCategories.AnyAsync(c => c.Id == model.CategoryId && c.IsActive))
        {
            return ServiceResult.Fail("Please select a valid category.", nameof(model.CategoryId));
        }

        var room = await _db.Rooms.AsNoTracking()
            .Where(r => r.Id == model.RoomId)
            .Select(r => new { r.BuildingId, r.IsActive, BuildingIsActive = r.Building.IsActive })
            .FirstOrDefaultAsync();

        if (room is null || !room.IsActive || !room.BuildingIsActive)
        {
            return ServiceResult.Fail("Please select a valid room.", nameof(model.RoomId));
        }
        if (room.BuildingId != model.BuildingId)
        {
            return ServiceResult.Fail("The selected room is not in the selected building.", nameof(model.RoomId));
        }
        return null;
    }

    private IQueryable<Issue> OpenIssues() =>
        _db.Issues.Where(i =>
            i.Status != IssueStatus.Closed &&
            i.Status != IssueStatus.Rejected &&
            i.Status != IssueStatus.Cancelled);

    private static IQueryable<Issue> Filter(IQueryable<Issue> query, IssueListFilter filter)
    {
        if (filter.Status is not null)
        {
            query = query.Where(i => i.Status == filter.Status);
        }
        if (filter.Priority is not null)
        {
            query = query.Where(i => i.Priority == filter.Priority);
        }
        if (filter.TeamId is not null)
        {
            query = query.Where(i => i.AssignedTeamId == filter.TeamId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            // "ISS-000123" or "123" finds issue 123 directly.
            var idText = term.StartsWith("ISS-", StringComparison.OrdinalIgnoreCase) ? term[4..] : term;
            var hasId = int.TryParse(idText, out var id);

            query = query.Where(i =>
                (hasId && i.Id == id) ||
                i.Title.Contains(term) ||
                i.Room.RoomNumber.Contains(term) ||
                i.Room.Building.Name.Contains(term));
        }

        return query;
    }

    /// <param name="staffView">true on staff/admin lists: also show reporter and assignee.</param>
    private static IQueryable<IssueListItem> ToListItems(IQueryable<Issue> query, bool staffView, bool oldestFirst = false)
    {
        var ordered = oldestFirst
            ? query.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id)
            : query.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id);

        return ordered.AsNoTracking()
            .Select(i => new IssueListItem(
                i.Id,
                i.Title,
                i.Category.Name,
                i.Room.Building.Name,
                i.Room.RoomNumber,
                i.Status,
                i.Priority,
                i.CreatedAt,
                staffView ? i.Reporter.FullName : null,
                staffView && i.AssignedTeam != null
                    ? i.AssignedTeam.Name + (i.AssignedStaff != null ? " · " + i.AssignedStaff.FullName : "")
                    : null));
    }
}
