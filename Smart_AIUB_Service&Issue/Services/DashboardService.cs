using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Dashboard;
using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.Services;

/// <summary>
/// Dashboard numbers. Every count is a COUNT(*) in SQL and every list takes only a
/// few rows, so the dashboards stay fast as the number of issues grows.
/// Tile links are app-relative ("/Issues?..."); the views turn them into real URLs.
/// </summary>
public class DashboardService : IDashboardService
{
    private const int ListSize = 5;

    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ---------- Reporter ----------

    public async Task<ReporterDashboardViewModel> GetReporterAsync(string userId)
    {
        var mine = _db.Issues.AsNoTracking().Where(i => i.ReporterId == userId);

        var waiting = await mine.CountAsync(i => i.Status == IssueStatus.Submitted);
        var beingFixed = await InWork(mine).CountAsync();
        var toConfirm = await mine.CountAsync(i => i.Status == IssueStatus.Resolved);
        var closed = await mine.CountAsync(i => i.Status == IssueStatus.Closed);

        return new ReporterDashboardViewModel
        {
            FullName = await NameAsync(userId),
            Tiles =
            [
                new("Waiting for review", waiting, "An admin will check them soon", "/Issues?status=Submitted", "indigo"),
                new("Being fixed", beingFixed, "Assigned, in progress or on hold", "/Issues", "blue"),
                new("Please confirm", toConfirm, "Resolved: is it really fixed?", "/Issues?status=Resolved", toConfirm > 0 ? "amber" : "green"),
                new("Closed", closed, "Fixed and confirmed", "/Issues?status=Closed", "green")
            ],
            NeedsConfirmation = await ToItems(
                mine.Where(i => i.Status == IssueStatus.Resolved).OrderBy(i => i.ResolvedAt).Take(ListSize), staffView: false),
            Recent = await ToItems(
                mine.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id).Take(ListSize), staffView: false)
        };
    }

    // ---------- Support staff ----------

    public async Task<StaffDashboardViewModel> GetStaffAsync(IssueActor actor)
    {
        var team = _db.Issues.AsNoTracking().Where(i => actor.TeamId != null && i.AssignedTeamId == actor.TeamId);
        var mine = team.Where(i => i.AssignedStaffId == actor.UserId);
        var unassigned = team.Where(i => i.Status == IssueStatus.Assigned && i.AssignedStaffId == null);
        var since = DateTime.UtcNow.AddDays(-30);

        var myOpen = await InWork(mine).CountAsync();
        var myOnHold = await mine.CountAsync(i => i.Status == IssueStatus.OnHold);
        var queue = await unassigned.CountAsync();
        var resolved30 = await mine.CountAsync(i => i.ResolvedAt != null && i.ResolvedAt >= since);

        return new StaffDashboardViewModel
        {
            FullName = await NameAsync(actor.UserId),
            TeamName = await _db.SupportTeams.Where(t => t.Id == actor.TeamId).Select(t => t.Name).FirstOrDefaultAsync(),
            Tiles =
            [
                new("Assigned to me", myOpen, "Not resolved yet", "/Issues/Team?scope=mine", "blue"),
                new("Unassigned in team", queue, "Take one from the queue", "/Issues/Team?scope=unassigned", queue > 0 ? "amber" : "green"),
                new("On hold", myOnHold, "Waiting for parts or access", "/Issues/Team?scope=mine&status=OnHold", "indigo"),
                new("Resolved (30 days)", resolved30, "By you", null, "green")
            ],
            // Most urgent first: Critical > High > Medium > Low, then oldest.
            MyActive = await ToItems(
                InWork(mine)
                    .OrderByDescending(i => i.Priority == IssuePriority.Critical)
                    .ThenByDescending(i => i.Priority == IssuePriority.High)
                    .ThenByDescending(i => i.Priority == IssuePriority.Medium)
                    .ThenBy(i => i.CreatedAt)
                    .Take(ListSize),
                staffView: true),
            Unassigned = await ToItems(unassigned.OrderBy(i => i.CreatedAt).Take(ListSize), staffView: true)
        };
    }

    // ---------- Admin ----------

    public async Task<AdminDashboardViewModel> GetAdminAsync()
    {
        var issues = _db.Issues.AsNoTracking();
        var since = DateTime.UtcNow.AddDays(-30);

        var awaitingReview = await issues.CountAsync(i => i.Status == IssueStatus.Submitted);
        var inWork = await InWork(issues).CountAsync();
        var urgent = await InWork(issues).CountAsync(i => i.Priority == IssuePriority.Critical || i.Priority == IssuePriority.High);
        var closed30 = await issues.CountAsync(i => i.ClosedAt != null && i.ClosedAt >= since);
        var brokenAssets = await _db.Assets.CountAsync(a => a.Status == AssetStatus.UnderMaintenance || a.Status == AssetStatus.OutOfService);

        // GROUP BY in SQL: we only receive one total per group.
        var byPriority = await InWork(issues)
            .GroupBy(i => i.Priority)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var byCategory = await Active(issues)
            .GroupBy(i => i.Category.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();

        var byBuilding = await Active(issues)
            .GroupBy(i => i.Room.Building.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();

        var byTeam = await InWork(issues)
            .Where(i => i.AssignedTeamId != null)
            .GroupBy(i => new { i.AssignedTeamId, i.AssignedTeam!.Name })
            .Select(g => new { g.Key.AssignedTeamId, g.Key.Name, Count = g.Count() })
            .ToListAsync();

        return new AdminDashboardViewModel
        {
            Tiles =
            [
                new("Waiting for review", awaitingReview, "Assign or reject", "/IssueReview", awaitingReview > 0 ? "amber" : "green"),
                new("Being worked on", inWork, "Assigned, in progress or on hold", "/IssueReview/All", "blue"),
                new("High & critical", urgent, "Urgent issues still in work", null, urgent > 0 ? "red" : "green"),
                new("Closed (30 days)", closed30, "Fixed and confirmed", "/IssueReview/All?status=Closed", "green"),
                new("Assets needing care", brokenAssets, "Under maintenance or out of service", "/Assets", brokenAssets > 0 ? "amber" : "green")
            ],
            // Fixed order Critical → Low so the bars are easy to compare.
            ByPriority = new BarList("Work in progress by priority",
                Enum.GetValues<IssuePriority>().Reverse()
                    .Select(p => new BarItem(p.ToString(), byPriority.FirstOrDefault(x => x.Key == p)?.Count ?? 0, $"/IssueReview/All?priority={p}"))
                    .ToList()),
            ByCategory = new BarList("Open issues by category",
                byCategory.OrderByDescending(x => x.Count).Take(6).Select(x => new BarItem(x.Name, x.Count)).ToList()),
            ByBuilding = new BarList("Open issues by building",
                byBuilding.OrderByDescending(x => x.Count).Take(6).Select(x => new BarItem(x.Name, x.Count)).ToList()),
            ByTeam = new BarList("Work in progress by team",
                byTeam.OrderByDescending(x => x.Count)
                    .Select(x => new BarItem(x.Name, x.Count, $"/IssueReview/All?teamId={x.AssignedTeamId}")).ToList()),
            OldestWaiting = await ToItems(
                issues.Where(i => i.Status == IssueStatus.Submitted).OrderBy(i => i.CreatedAt).Take(ListSize), staffView: true),
            RecentlyResolved = await ToItems(
                issues.Where(i => i.ResolvedAt != null).OrderByDescending(i => i.ResolvedAt).Take(ListSize), staffView: true)
        };
    }

    // ---------- Helpers ----------

    /// <summary>Assigned, In progress or On hold: someone is (or should be) working on it.</summary>
    private static IQueryable<Issue> InWork(IQueryable<Issue> query) =>
        query.Where(i => i.Status == IssueStatus.Assigned || i.Status == IssueStatus.InProgress || i.Status == IssueStatus.OnHold);

    /// <summary>Still needs attention: Submitted or in work (not resolved, not finished).</summary>
    private static IQueryable<Issue> Active(IQueryable<Issue> query) =>
        query.Where(i => i.Status == IssueStatus.Submitted || i.Status == IssueStatus.Assigned ||
                         i.Status == IssueStatus.InProgress || i.Status == IssueStatus.OnHold);

    private Task<string> NameAsync(string userId) =>
        _db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstAsync();

    /// <summary>Projects an already sorted and limited query into list rows.</summary>
    private static Task<List<IssueListItem>> ToItems(IQueryable<Issue> query, bool staffView) =>
        query.Select(i => new IssueListItem(
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
                    : null))
            .ToListAsync();
}
