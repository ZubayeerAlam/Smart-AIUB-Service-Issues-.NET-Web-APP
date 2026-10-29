using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.Tests.Services;

public class IssueServiceTests
{
    [Fact]
    public async Task Report_creates_submitted_issue_with_first_history_entry()
    {
        await using var data = await IssueTestData.CreateAsync();

        var id = await data.ReportAsync();

        var db = data.Services.Get<ApplicationDbContext>();
        var issue = await db.Issues.Include(i => i.StatusHistory).SingleAsync(i => i.Id == id);
        Assert.Equal(IssueStatus.Submitted, issue.Status);
        Assert.Null(issue.Priority);
        Assert.Null(issue.AssignedTeamId);
        Assert.Equal(data.Reporter.Id, issue.ReporterId);
        var history = Assert.Single(issue.StatusHistory);
        Assert.Null(history.FromStatus);
        Assert.Equal(IssueStatus.Submitted, history.ToStatus);
    }

    [Fact]
    public async Task Report_rejects_room_that_is_not_in_the_selected_building()
    {
        await using var data = await IssueTestData.CreateAsync();
        var form = data.ValidForm();
        form.BuildingId = data.C.Id; // room D301 is in D-Building, not C-Building

        var result = await data.Services.Get<IIssueService>().ReportAsync(form, data.Reporter.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(IssueFormViewModel.RoomId), result.Field);
        Assert.Empty(data.Services.Get<ApplicationDbContext>().Issues);
    }

    [Fact]
    public async Task Report_rejects_inactive_room_and_inactive_category()
    {
        await using var data = await IssueTestData.CreateAsync();
        var db = data.Services.Get<ApplicationDbContext>();
        data.D301.IsActive = false;
        await db.SaveChangesAsync();

        var roomResult = await data.Services.Get<IIssueService>().ReportAsync(data.ValidForm(), data.Reporter.Id);

        data.D301.IsActive = true;
        data.Projector.IsActive = false;
        await db.SaveChangesAsync();
        var categoryResult = await data.Services.Get<IIssueService>().ReportAsync(data.ValidForm(), data.Reporter.Id);

        Assert.Equal(nameof(IssueFormViewModel.RoomId), roomResult.Field);
        Assert.Equal(nameof(IssueFormViewModel.CategoryId), categoryResult.Field);
    }

    [Fact]
    public async Task Reporter_can_edit_while_submitted()
    {
        await using var data = await IssueTestData.CreateAsync();
        var id = await data.ReportAsync();
        var form = data.ValidForm(data.C101);
        form.Id = id;
        form.Title = "Updated title";

        var result = await data.Services.Get<IIssueService>()
            .UpdateAsync(form, data.ActorFor(data.Reporter, Roles.Student));

        Assert.True(result.Succeeded, result.Error);
        var issue = await data.Services.Get<ApplicationDbContext>().Issues.AsNoTracking().SingleAsync();
        Assert.Equal("Updated title", issue.Title);
        Assert.Equal(data.C101.Id, issue.RoomId);
    }

    [Theory]
    [InlineData(IssueStatus.Assigned)]
    [InlineData(IssueStatus.InProgress)]
    [InlineData(IssueStatus.Resolved)]
    public async Task Edit_is_refused_after_review(IssueStatus status)
    {
        await using var data = await IssueTestData.CreateAsync();
        var id = await data.ReportAsync();
        await data.SetStateAsync(id, status, data.ItTeam);
        var form = data.ValidForm();
        form.Id = id;
        form.Title = "Sneaky change";

        var result = await data.Services.Get<IIssueService>()
            .UpdateAsync(form, data.ActorFor(data.Reporter, Roles.Student));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Another_user_cannot_edit_my_issue()
    {
        await using var data = await IssueTestData.CreateAsync();
        var id = await data.ReportAsync();
        var form = data.ValidForm();
        form.Id = id;

        var result = await data.Services.Get<IIssueService>()
            .UpdateAsync(form, data.ActorFor(data.OtherReporter, Roles.Faculty));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Cancel_by_reporter_sets_Cancelled_and_adds_history()
    {
        await using var data = await IssueTestData.CreateAsync();
        var id = await data.ReportAsync();

        var result = await data.Services.Get<IIssueWorkflowService>()
            .CancelAsync(id, "Works again", data.ActorFor(data.Reporter, Roles.Student));

        Assert.True(result.Succeeded, result.Error);
        var db = data.Services.Get<ApplicationDbContext>();
        var issue = await db.Issues.AsNoTracking().Include(i => i.StatusHistory).SingleAsync();
        Assert.Equal(IssueStatus.Cancelled, issue.Status);
        Assert.Equal(2, issue.StatusHistory.Count);
        Assert.Contains(issue.StatusHistory, h => h.ToStatus == IssueStatus.Cancelled && h.Note == "Works again");
    }

    [Fact]
    public async Task Cancel_is_refused_after_review()
    {
        await using var data = await IssueTestData.CreateAsync();
        var id = await data.ReportAsync();
        await data.SetStateAsync(id, IssueStatus.Assigned, data.ItTeam);

        var result = await data.Services.Get<IIssueWorkflowService>()
            .CancelAsync(id, null, data.ActorFor(data.Reporter, Roles.Student));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task MyIssues_only_lists_my_own_issues()
    {
        await using var data = await IssueTestData.CreateAsync();
        await data.ReportAsync();
        await data.Services.Get<IIssueService>().ReportAsync(data.ValidForm(), data.OtherReporter.Id);

        var mine = await data.Services.Get<IIssueService>().GetMyIssuesAsync(data.Reporter.Id, new IssueListFilter(), 1);

        Assert.Single(mine.Items);
    }

    [Fact]
    public async Task OpenIssues_hides_finished_issues_and_reporter_names()
    {
        await using var data = await IssueTestData.CreateAsync();
        var open = await data.ReportAsync();
        var closed = await data.ReportAsync();
        var cancelled = await data.ReportAsync();
        await data.SetStateAsync(closed, IssueStatus.Closed);
        await data.SetStateAsync(cancelled, IssueStatus.Cancelled);

        var list = await data.Services.Get<IIssueService>().GetOpenIssuesAsync(new IssueListFilter(), 1);

        var item = Assert.Single(list.Items);
        Assert.Equal(open, item.Id);
        Assert.Null(item.ReporterName);
    }

    [Fact]
    public async Task Search_finds_issue_by_ticket_number()
    {
        await using var data = await IssueTestData.CreateAsync();
        var id = await data.ReportAsync();
        await data.ReportAsync();

        var found = await data.Services.Get<IIssueService>()
            .GetMyIssuesAsync(data.Reporter.Id, new IssueListFilter { Search = $"ISS-{id:000000}" }, 1);

        Assert.Equal(id, Assert.Single(found.Items).Id);
    }
}
