using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.Tests.Services;

/// <summary>Date-range filtering and reports, especially the UTC ↔ Asia/Dhaka day boundaries.</summary>
public class ReportServiceTests
{
    private static readonly DateOnly Oct1 = new(2026, 10, 1);
    private static readonly DateOnly Oct31 = new(2026, 10, 31);
    private static readonly DateOnly Nov1 = new(2026, 11, 1);
    private static readonly DateOnly Nov30 = new(2026, 11, 30);

    private static DateTime Utc(int m, int d, int h, int min, int s = 0) => new(2026, m, d, h, min, s, DateTimeKind.Utc);

    /// <summary>Reports an issue and moves its CreatedAt to the given UTC moment.</summary>
    private static async Task<int> IssueCreatedAtAsync(IssueTestData d, DateTime createdUtc, DateTime? resolvedUtc = null)
    {
        var id = await d.ReportAsync();
        var db = d.Services.Get<ApplicationDbContext>();
        var issue = await db.Issues.FindAsync(id);
        issue!.CreatedAt = createdUtc;
        if (resolvedUtc is not null)
        {
            issue.Status = IssueStatus.Resolved;
            issue.ResolvedAt = resolvedUtc;
        }
        await db.SaveChangesAsync();
        return id;
    }

    /// <summary>The four moments around both edges of October (Dhaka).</summary>
    private static async Task<(int BeforeOct, int FirstMinuteOfOct, int LastSecondOfOct, int FirstMinuteOfNov)> SeedEdgesAsync(IssueTestData d) =>
    (
        await IssueCreatedAtAsync(d, Utc(9, 30, 17, 59, 59)),  // 30 Sep 23:59:59 Dhaka
        await IssueCreatedAtAsync(d, Utc(9, 30, 18, 0, 0)),    //  1 Oct 00:00:00 Dhaka
        await IssueCreatedAtAsync(d, Utc(10, 31, 17, 59, 59)), // 31 Oct 23:59:59 Dhaka
        await IssueCreatedAtAsync(d, Utc(10, 31, 18, 0, 0))    //  1 Nov 00:00:00 Dhaka
    );

    [Fact]
    public async Task Report_for_October_includes_exactly_the_Dhaka_days_of_October()
    {
        await using var d = await IssueTestData.CreateAsync();
        await SeedEdgesAsync(d);
        var reports = d.Services.Get<IReportService>();

        var october = await reports.GetAsync(Oct1, Oct31);
        var november = await reports.GetAsync(Nov1, Nov30);

        Assert.Equal(2, october.IssuesReported);   // 1 Oct 00:00 and 31 Oct 23:59:59
        Assert.Equal(1, november.IssuesReported);  // 1 Nov 00:00
        Assert.Equal(31, october.Days);
    }

    [Fact]
    public async Task All_issues_date_filter_uses_the_same_Dhaka_boundaries()
    {
        await using var d = await IssueTestData.CreateAsync();
        var edges = await SeedEdgesAsync(d);

        var page = await d.Services.Get<IIssueService>()
            .GetAllIssuesAsync(new IssueListFilter { From = Oct1, To = Oct31 }, 1);

        Assert.Equal(
            new[] { edges.FirstMinuteOfOct, edges.LastSecondOfOct }.Order(),
            page.Items.Select(i => i.Id).Order());
    }

    [Fact]
    public async Task Single_day_filter_finds_issues_reported_that_Dhaka_day()
    {
        await using var d = await IssueTestData.CreateAsync();
        var edges = await SeedEdgesAsync(d);

        var page = await d.Services.Get<IIssueService>()
            .GetAllIssuesAsync(new IssueListFilter { From = Nov1, To = Nov1 }, 1);

        Assert.Equal(edges.FirstMinuteOfNov, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task Building_and_category_filters_narrow_the_list()
    {
        await using var d = await IssueTestData.CreateAsync();
        var inD = await d.ReportAsync();
        var inC = (await d.Services.Get<IIssueService>().ReportAsync(d.ValidForm(d.C101), d.Reporter.Id)).Value;
        var issues = d.Services.Get<IIssueService>();

        var dOnly = await issues.GetAllIssuesAsync(new IssueListFilter { BuildingId = d.D.Id }, 1);
        var cOnly = await issues.GetMyIssuesAsync(d.Reporter.Id, new IssueListFilter { BuildingId = d.C.Id, CategoryId = d.Projector.Id }, 1);

        Assert.Equal(inD, Assert.Single(dOnly.Items).Id);
        Assert.Equal(inC, Assert.Single(cOnly.Items).Id);
    }

    [Fact]
    public async Task Resolution_time_average_and_median_count_issues_resolved_in_the_range()
    {
        await using var d = await IssueTestData.CreateAsync();
        var created = Utc(10, 10, 4, 0);  // 10 Oct 10:00 Dhaka
        await IssueCreatedAtAsync(d, created, created.AddHours(2));
        await IssueCreatedAtAsync(d, created, created.AddHours(6));
        await IssueCreatedAtAsync(d, created, created.AddHours(16));
        await IssueCreatedAtAsync(d, created, Utc(11, 5, 4, 0)); // resolved in November: not counted

        var report = await d.Services.Get<IReportService>().GetAsync(Oct1, Oct31);

        Assert.Equal(3, report.ResolvedCount);
        Assert.Equal(8, report.AverageHours!.Value, precision: 6); // (2 + 6 + 16) / 3
        Assert.Equal(6, report.MedianHours!.Value, precision: 6);
    }

    [Fact]
    public async Task Maintenance_cost_adds_up_records_in_the_range_only()
    {
        await using var d = await IssueTestData.CreateAsync();
        var assetId = await d.CreateAssetAsync("PRJ-D301");
        var db = d.Services.Get<ApplicationDbContext>();
        MaintenanceRecord Record(DateTime performedUtc, decimal? cost, MaintenanceType type) => new()
        {
            AssetId = assetId, PerformedById = d.ItStaff.Id, Type = type, Description = "Work",
            PerformedAt = performedUtc, Cost = cost, CreatedAt = performedUtc
        };
        db.MaintenanceRecords.AddRange(
            Record(Utc(9, 30, 18, 0), 1000m, MaintenanceType.Corrective),  // 1 Oct 00:00 Dhaka: in
            Record(Utc(10, 15, 6, 0), 500.50m, MaintenanceType.Preventive),
            Record(Utc(10, 20, 6, 0), null, MaintenanceType.Inspection),   // no cost
            Record(Utc(10, 31, 18, 0), 9999m, MaintenanceType.Corrective)); // 1 Nov 00:00 Dhaka: out
        await db.SaveChangesAsync();

        var report = await d.Services.Get<IReportService>().GetAsync(Oct1, Oct31);

        Assert.Equal(3, report.MaintenanceCount);
        Assert.Equal(1500.50m, report.MaintenanceCost);
        Assert.Equal(1000m, report.CostByMaintenanceType.Single(r => r.Label == "Corrective").Cost);
        Assert.Equal(1500.50m, Assert.Single(report.TopAssetsByCost).Cost);
    }

    [Fact]
    public async Task Report_rejects_a_reversed_range()
    {
        await using var d = await IssueTestData.CreateAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => d.Services.Get<IReportService>().GetAsync(Oct31, Oct1));
    }
}
