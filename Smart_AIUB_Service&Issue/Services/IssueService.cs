using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

    public IssueService(ApplicationDbContext db)
    {
        _db = db;
    }

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

    public Task<PagedResult<IssueListItem>> GetMyIssuesAsync(string userId, string? search, IssueStatus? status, int page) =>
        ToListItems(Filter(_db.Issues.Where(i => i.ReporterId == userId), search, status), includeReporter: false)
            .ToPagedResultAsync(page);

    public Task<PagedResult<IssueListItem>> GetOpenIssuesAsync(string? search, IssueStatus? status, int page) =>
        ToListItems(Filter(_db.Issues.Where(i =>
                i.Status != IssueStatus.Closed &&
                i.Status != IssueStatus.Rejected &&
                i.Status != IssueStatus.Cancelled), search, status), includeReporter: false)
            .ToPagedResultAsync(page);

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
                UpdatedAt = i.UpdatedAt
            })
            .FirstAsync();

        model.History = await _db.IssueStatusHistory.AsNoTracking()
            .Where(h => h.IssueId == id)
            .OrderBy(h => h.ChangedAt).ThenBy(h => h.Id)
            .Select(h => new IssueHistoryItem(h.FromStatus, h.ToStatus, h.ChangedBy.FullName, h.ChangedAt, h.Note))
            .ToListAsync();

        model.IsReporter = IssuePermissions.IsReporter(issue, actor);
        model.CanEdit = IssuePermissions.CanEdit(issue, actor);
        model.CanCancel = IssuePermissions.CanCancel(issue, actor);
        return model;
    }

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

    private static IQueryable<Issue> Filter(IQueryable<Issue> query, string? search, IssueStatus? status)
    {
        if (status is not null)
        {
            query = query.Where(i => i.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
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

    private static IQueryable<IssueListItem> ToListItems(IQueryable<Issue> query, bool includeReporter) =>
        query.AsNoTracking()
            .OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id)
            .Select(i => new IssueListItem(
                i.Id,
                i.Title,
                i.Category.Name,
                i.Room.Building.Name,
                i.Room.RoomNumber,
                i.Status,
                i.Priority,
                i.CreatedAt,
                includeReporter ? i.Reporter.FullName : null));
}
