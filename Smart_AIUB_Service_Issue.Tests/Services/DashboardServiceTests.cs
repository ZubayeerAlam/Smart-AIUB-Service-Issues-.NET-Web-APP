using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Dashboard;

namespace Smart_AIUB_Service_Issue.Tests.Services;

public class DashboardServiceTests
{
    private static int Tile(IEnumerable<StatTile> tiles, string label) => tiles.Single(t => t.Label == label).Value;

    /// <summary>
    /// A small campus story: Reporter has 3 issues (one waiting, one with IT, one resolved),
    /// OtherReporter has 1 waiting issue. Returns the issue ids.
    /// </summary>
    private static async Task<(int Waiting, int WithIt, int Resolved, int OthersWaiting)> SeedAsync(IssueTestData d)
    {
        var wf = d.Services.Get<IIssueWorkflowService>();
        var admin = d.ActorFor(d.Admin, Roles.Admin);
        var it = d.ActorFor(d.ItStaff, Roles.SupportStaff);

        var waiting = await d.ReportAsync();

        var withIt = await d.ReportAsync();
        Assert.True((await wf.AssignAsync(withIt, IssuePriority.High, d.ItTeam.Id, null, null, admin)).Succeeded);

        var resolved = await d.ReportAsync();
        Assert.True((await wf.AssignAsync(resolved, IssuePriority.Low, d.ItTeam.Id, d.ItStaff.Id, null, admin)).Succeeded);
        Assert.True((await wf.StartAsync(resolved, it)).Succeeded);
        Assert.True((await wf.ResolveAsync(resolved, "Replaced the projector lamp.", it)).Succeeded);

        var othersWaiting = (await d.Services.Get<IIssueService>().ReportAsync(d.ValidForm(), d.OtherReporter.Id)).Value;
        return (waiting, withIt, resolved, othersWaiting);
    }

    [Fact]
    public async Task Reporter_dashboard_counts_only_own_issues()
    {
        await using var d = await IssueTestData.CreateAsync();
        var ids = await SeedAsync(d);

        var model = await d.Services.Get<IDashboardService>().GetReporterAsync(d.Reporter.Id);

        Assert.Equal(1, Tile(model.Tiles, "Waiting for review"));   // not OtherReporter's
        Assert.Equal(1, Tile(model.Tiles, "Being fixed"));
        Assert.Equal(1, Tile(model.Tiles, "Please confirm"));
        Assert.Equal(0, Tile(model.Tiles, "Closed"));
        Assert.Equal(ids.Resolved, Assert.Single(model.NeedsConfirmation).Id);
        Assert.Equal(3, model.Recent.Count);
        Assert.All(model.Recent, i => Assert.Null(i.ReporterName)); // reporters never see names
    }

    [Fact]
    public async Task Staff_dashboard_shows_own_work_and_team_queue()
    {
        await using var d = await IssueTestData.CreateAsync();
        var ids = await SeedAsync(d);
        var dashboards = d.Services.Get<IDashboardService>();

        var it = await dashboards.GetStaffAsync(d.ActorFor(d.ItStaff, Roles.SupportStaff));
        var elec = await dashboards.GetStaffAsync(d.ActorFor(d.ElectricalStaff, Roles.SupportStaff));

        Assert.Equal("IT Support", it.TeamName);
        Assert.Equal(0, Tile(it.Tiles, "Assigned to me"));          // the resolved one no longer counts
        Assert.Equal(1, Tile(it.Tiles, "Unassigned in team"));
        Assert.Equal(1, Tile(it.Tiles, "Resolved (30 days)"));
        Assert.Equal(ids.WithIt, Assert.Single(it.Unassigned).Id);

        Assert.All(elec.Tiles, t => Assert.Equal(0, t.Value));      // other team sees nothing
        Assert.Empty(elec.Unassigned);
    }

    [Fact]
    public async Task Admin_dashboard_counts_and_groups_the_whole_campus()
    {
        await using var d = await IssueTestData.CreateAsync();
        var ids = await SeedAsync(d);

        var model = await d.Services.Get<IDashboardService>().GetAdminAsync();

        Assert.Equal(2, Tile(model.Tiles, "Waiting for review"));    // Reporter's + OtherReporter's
        Assert.Equal(1, Tile(model.Tiles, "Being worked on"));
        Assert.Equal(1, Tile(model.Tiles, "High & critical"));

        Assert.Equal(1, model.ByPriority.Items.Single(b => b.Label == "High").Count);
        Assert.Equal(0, model.ByPriority.Items.Single(b => b.Label == "Low").Count); // resolved: not in work
        Assert.Equal(1, Assert.Single(model.ByTeam.Items).Count);
        Assert.Equal(3, model.ByBuilding.Items.Single(b => b.Label == "D-Building").Count); // 2 waiting + 1 in work

        Assert.Equal(new[] { ids.Waiting, ids.OthersWaiting }, model.OldestWaiting.Select(i => i.Id));
        Assert.Equal(ids.Resolved, Assert.Single(model.RecentlyResolved).Id);
    }
}
