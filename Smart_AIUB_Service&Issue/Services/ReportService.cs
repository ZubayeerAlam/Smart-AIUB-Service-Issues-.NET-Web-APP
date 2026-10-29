using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Dashboard;
using Smart_AIUB_Service_Issue.ViewModels.Reports;

namespace Smart_AIUB_Service_Issue.Services;

/// <summary>
/// Date-range reports. Counts per group are done in SQL (GROUP BY). Times and money are
/// added up in C# after loading only the few columns needed: date differences and decimal
/// sums are written differently in every database, so this keeps the code simple and
/// identical on SQL Server and on SQLite (tests).
/// </summary>
public class ReportService : IReportService
{
    private const int TopCount = 8;

    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ReportViewModel> GetAsync(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new ArgumentException("'from' must not be after 'to'.");
        }

        // 1 Oct–31 Oct in Dhaka  =>  30 Sep 18:00 UTC (inclusive) to 31 Oct 18:00 UTC (exclusive).
        var (startUtc, endUtc) = DhakaTime.ToUtcRange(from, to);
        var start = startUtc!.Value;
        var end = endUtc!.Value;

        var model = new ReportViewModel { From = from, To = to };

        // ---------- Issues reported in the range ----------
        var reported = _db.Issues.AsNoTracking().Where(i => i.CreatedAt >= start && i.CreatedAt < end);
        model.IssuesReported = await reported.CountAsync();

        var byStatus = await reported.GroupBy(i => i.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
        model.ByStatus = new BarList("Reported issues by current status",
            Enum.GetValues<IssueStatus>()
                .Select(s => new BarItem(s.DisplayName(), byStatus.FirstOrDefault(x => x.Key == s)?.Count ?? 0))
                .Where(b => b.Count > 0)
                .ToList());

        model.ByCategory = await BarsAsync("By category",
            reported.GroupBy(i => i.Category.Name).Select(g => new BarItem(g.Key, g.Count(), null)));
        model.ByBuilding = await BarsAsync("By building",
            reported.GroupBy(i => i.Room.Building.Name).Select(g => new BarItem(g.Key, g.Count(), null)));
        model.ByTeam = await BarsAsync("By assigned team",
            reported.Where(i => i.AssignedTeamId != null)
                .GroupBy(i => i.AssignedTeam!.Name).Select(g => new BarItem(g.Key, g.Count(), null)));

        // ---------- Issues resolved in the range ----------
        var resolved = await _db.Issues.AsNoTracking()
            .Where(i => i.ResolvedAt != null && i.ResolvedAt >= start && i.ResolvedAt < end)
            .Select(i => new { i.CreatedAt, ResolvedAt = i.ResolvedAt!.Value, i.Priority, Category = i.Category.Name })
            .ToListAsync();

        var withHours = resolved.Select(r => new
        {
            r.Priority,
            r.Category,
            Hours = (r.ResolvedAt - r.CreatedAt).TotalHours
        }).ToList();

        model.ResolvedCount = withHours.Count;
        if (withHours.Count > 0)
        {
            model.AverageHours = withHours.Average(r => r.Hours);
            model.MedianHours = Median(withHours.Select(r => r.Hours));
        }

        model.ResolutionByPriority = Enum.GetValues<IssuePriority>().Reverse()
            .Select(p => withHours.Where(r => r.Priority == p).ToList())
            .Where(g => g.Count > 0)
            .Select(g => new ResolutionRow(g[0].Priority.ToString()!, g.Count, g.Average(r => r.Hours)))
            .ToList();

        model.ResolutionByCategory = withHours
            .GroupBy(r => r.Category)
            .Select(g => new ResolutionRow(g.Key, g.Count(), g.Average(r => r.Hours)))
            .OrderByDescending(r => r.AverageHours)
            .ToList();

        // ---------- Maintenance performed in the range ----------
        var maintenance = await _db.MaintenanceRecords.AsNoTracking()
            .Where(m => m.PerformedAt >= start && m.PerformedAt < end)
            .Select(m => new { m.Type, AssetType = m.Asset.AssetType.Name, Asset = m.Asset.AssetTag + " · " + m.Asset.Name, m.Cost })
            .ToListAsync();

        model.MaintenanceCount = maintenance.Count;
        model.MaintenanceCost = maintenance.Sum(m => m.Cost ?? 0);

        model.CostByMaintenanceType = maintenance
            .GroupBy(m => m.Type.ToString())
            .Select(g => new CostRow(g.Key, g.Count(), g.Sum(m => m.Cost ?? 0)))
            .OrderByDescending(r => r.Cost)
            .ToList();

        model.CostByAssetType = maintenance
            .GroupBy(m => m.AssetType)
            .Select(g => new CostRow(g.Key, g.Count(), g.Sum(m => m.Cost ?? 0)))
            .OrderByDescending(r => r.Cost)
            .ToList();

        model.TopAssetsByCost = maintenance
            .GroupBy(m => m.Asset)
            .Select(g => new CostRow(g.Key, g.Count(), g.Sum(m => m.Cost ?? 0)))
            .OrderByDescending(r => r.Cost).ThenByDescending(r => r.Count)
            .Take(TopCount)
            .ToList();

        return model;
    }

    private static async Task<BarList> BarsAsync(string title, IQueryable<BarItem> query)
    {
        var items = await query.ToListAsync();
        return new BarList(title, items.OrderByDescending(i => i.Count).Take(TopCount).ToList());
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }
}
