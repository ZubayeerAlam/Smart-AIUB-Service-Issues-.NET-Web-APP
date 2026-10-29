using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.Tests.Services;

public class IssueCommentTests
{
    private static async Task<int> AssignedToItAsync(IssueTestData d)
    {
        var id = await d.ReportAsync();
        var result = await d.Services.Get<IIssueWorkflowService>()
            .AssignAsync(id, IssuePriority.High, d.ItTeam.Id, null, null, d.ActorFor(d.Admin, Roles.Admin));
        Assert.True(result.Succeeded, result.Error);
        return id;
    }

    private static Task<Common.ServiceResult> CommentAsync(IssueTestData d, int id, string body, bool isInternal, IssueActor actor) =>
        d.Services.Get<IIssueService>().AddCommentAsync(id, new AddCommentViewModel { Body = body, IsInternal = isInternal }, actor);

    [Fact]
    public async Task Internal_notes_are_hidden_from_the_reporter_but_visible_to_team_and_admin()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await AssignedToItAsync(d);
        var it = d.ActorFor(d.ItStaff, Roles.SupportStaff);
        Assert.True((await CommentAsync(d, id, "We'll come at 2 PM.", isInternal: false, it)).Succeeded);
        Assert.True((await CommentAsync(d, id, "Lamp is out of warranty, order a new one.", isInternal: true, it)).Succeeded);
        var issues = d.Services.Get<IIssueService>();

        var asReporter = await issues.GetDetailsAsync(id, d.ActorFor(d.Reporter, Roles.Student));
        var asStaff = await issues.GetDetailsAsync(id, it);
        var asAdmin = await issues.GetDetailsAsync(id, d.ActorFor(d.Admin, Roles.Admin));

        Assert.Single(asReporter!.Comments);
        Assert.DoesNotContain(asReporter.Comments, c => c.IsInternal);
        Assert.Equal(2, asStaff!.Comments.Count);
        Assert.Equal(2, asAdmin!.Comments.Count);
    }

    [Fact]
    public async Task Reporter_cannot_write_internal_notes()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await AssignedToItAsync(d);

        var result = await CommentAsync(d, id, "Psst", isInternal: true, d.ActorFor(d.Reporter, Roles.Student));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Reporter_can_comment_on_own_issue()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await d.ReportAsync();

        var result = await CommentAsync(d, id, "It's room 301, the one near the stairs.", isInternal: false,
            d.ActorFor(d.Reporter, Roles.Student));

        Assert.True(result.Succeeded, result.Error);
    }

    [Fact]
    public async Task Strangers_and_other_teams_cannot_comment()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await AssignedToItAsync(d);

        Assert.False((await CommentAsync(d, id, "Hi", false, d.ActorFor(d.OtherReporter, Roles.Faculty))).Succeeded);
        Assert.False((await CommentAsync(d, id, "Hi", false, d.ActorFor(d.ElectricalStaff, Roles.SupportStaff))).Succeeded);
    }

    [Fact]
    public async Task Comments_are_closed_on_finished_issues()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await d.ReportAsync();
        var reporter = d.ActorFor(d.Reporter, Roles.Student);
        Assert.True((await d.Services.Get<IIssueWorkflowService>().CancelAsync(id, null, reporter)).Succeeded);

        var result = await CommentAsync(d, id, "One more thing", false, reporter);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Team_queue_unassigned_scope_lists_only_issues_nobody_took()
    {
        await using var d = await IssueTestData.CreateAsync();
        var free = await AssignedToItAsync(d);
        var taken = await AssignedToItAsync(d);
        var it = d.ActorFor(d.ItStaff, Roles.SupportStaff);
        Assert.True((await d.Services.Get<IIssueWorkflowService>().TakeAsync(taken, it)).Succeeded);
        var issues = d.Services.Get<IIssueService>();

        var unassigned = await issues.GetTeamIssuesAsync(it, TeamQueueScope.Unassigned, new IssueListFilter(), 1);
        var mine = await issues.GetTeamIssuesAsync(it, TeamQueueScope.Mine, new IssueListFilter(), 1);
        var otherTeam = await issues.GetTeamIssuesAsync(
            d.ActorFor(d.ElectricalStaff, Roles.SupportStaff), null, new IssueListFilter(), 1);

        Assert.Equal(free, Assert.Single(unassigned.Items).Id);
        Assert.Equal(taken, Assert.Single(mine.Items).Id);
        Assert.Empty(otherTeam.Items);
    }
}
