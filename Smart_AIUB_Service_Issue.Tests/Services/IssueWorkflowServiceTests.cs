using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;

namespace Smart_AIUB_Service_Issue.Tests.Services;

/// <summary>Every allowed transition from plan section 5, and the important forbidden ones.</summary>
public class IssueWorkflowServiceTests
{
    // Short names for the people in IssueTestData.
    private static IssueActor Reporter(IssueTestData d) => d.ActorFor(d.Reporter, Roles.Student);
    private static IssueActor Other(IssueTestData d) => d.ActorFor(d.OtherReporter, Roles.Faculty);
    private static IssueActor It(IssueTestData d) => d.ActorFor(d.ItStaff, Roles.SupportStaff);
    private static IssueActor Elec(IssueTestData d) => d.ActorFor(d.ElectricalStaff, Roles.SupportStaff);
    private static IssueActor Admin(IssueTestData d) => d.ActorFor(d.Admin, Roles.Admin);

    private static IIssueWorkflowService Workflow(IssueTestData d) => d.Services.Get<IIssueWorkflowService>();

    private static async Task<Issue> ReloadAsync(IssueTestData d, int id) =>
        await d.Services.Get<ApplicationDbContext>().Issues.AsNoTracking().SingleAsync(i => i.Id == id);

    /// <summary>Reported → assigned to IT (no technician).</summary>
    private static async Task<int> AssignedToItAsync(IssueTestData d)
    {
        var id = await d.ReportAsync();
        AssertOk(await Workflow(d).AssignAsync(id, IssuePriority.High, d.ItTeam.Id, null, null, Admin(d)));
        return id;
    }

    /// <summary>Assigned to IT, taken and started by the IT technician.</summary>
    private static async Task<int> InProgressAsync(IssueTestData d)
    {
        var id = await AssignedToItAsync(d);
        AssertOk(await Workflow(d).StartAsync(id, It(d)));
        return id;
    }

    private static async Task<int> ResolvedAsync(IssueTestData d)
    {
        var id = await InProgressAsync(d);
        AssertOk(await Workflow(d).ResolveAsync(id, "Replaced the projector lamp.", It(d)));
        return id;
    }

    private static void AssertOk(Common.ServiceResult result) => Assert.True(result.Succeeded, result.Error);

    // ---------- Happy path ----------

    [Fact]
    public async Task Full_lifecycle_writes_one_history_row_per_step()
    {
        await using var d = await IssueTestData.CreateAsync();
        var wf = Workflow(d);
        var id = await d.ReportAsync();

        AssertOk(await wf.AssignAsync(id, IssuePriority.High, d.ItTeam.Id, null, "Urgent: exam tomorrow", Admin(d)));
        AssertOk(await wf.TakeAsync(id, It(d)));
        AssertOk(await wf.StartAsync(id, It(d)));
        AssertOk(await wf.HoldAsync(id, "Waiting for a new lamp", It(d)));
        AssertOk(await wf.ResumeAsync(id, It(d)));
        AssertOk(await wf.ResolveAsync(id, "Replaced the projector lamp.", It(d)));
        AssertOk(await wf.CloseAsync(id, Reporter(d)));

        var issue = await ReloadAsync(d, id);
        Assert.Equal(IssueStatus.Closed, issue.Status);
        Assert.Equal(IssuePriority.High, issue.Priority);
        Assert.Equal(d.ItTeam.Id, issue.AssignedTeamId);
        Assert.Equal(d.ItStaff.Id, issue.AssignedStaffId);
        Assert.NotNull(issue.ResolvedAt);
        Assert.NotNull(issue.ClosedAt);
        Assert.Equal("Replaced the projector lamp.", issue.ResolutionNote);

        var history = await d.Services.Get<ApplicationDbContext>().IssueStatusHistory
            .Where(h => h.IssueId == id).OrderBy(h => h.Id).Select(h => h.ToStatus).ToListAsync();
        Assert.Equal(
            [IssueStatus.Submitted, IssueStatus.Assigned, IssueStatus.Assigned, IssueStatus.InProgress,
             IssueStatus.OnHold, IssueStatus.InProgress, IssueStatus.Resolved, IssueStatus.Closed],
            history);
    }

    // ---------- Admin review ----------

    [Fact]
    public async Task Assign_requires_priority_and_active_team()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await d.ReportAsync();

        var noPriority = await Workflow(d).AssignAsync(id, null, d.ItTeam.Id, null, null, Admin(d));
        var noTeam = await Workflow(d).AssignAsync(id, IssuePriority.Low, null, null, null, Admin(d));

        Assert.Equal("Priority", noPriority.Field);
        Assert.Equal("TeamId", noTeam.Field);
        Assert.Equal(IssueStatus.Submitted, (await ReloadAsync(d, id)).Status);
    }

    [Fact]
    public async Task Assign_rejects_technician_from_another_team()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await d.ReportAsync();

        var result = await Workflow(d).AssignAsync(id, IssuePriority.Low, d.ItTeam.Id, d.ElectricalStaff.Id, null, Admin(d));

        Assert.Equal("StaffId", result.Field);
    }

    [Fact]
    public async Task Assign_with_technician_from_the_team_succeeds()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await d.ReportAsync();

        AssertOk(await Workflow(d).AssignAsync(id, IssuePriority.Medium, d.ItTeam.Id, d.ItStaff.Id, null, Admin(d)));

        Assert.Equal(d.ItStaff.Id, (await ReloadAsync(d, id)).AssignedStaffId);
    }

    [Fact]
    public async Task Only_admin_can_assign_or_reject()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await d.ReportAsync();

        Assert.False((await Workflow(d).AssignAsync(id, IssuePriority.Low, d.ItTeam.Id, null, null, It(d))).Succeeded);
        Assert.False((await Workflow(d).AssignAsync(id, IssuePriority.Low, d.ItTeam.Id, null, null, Reporter(d))).Succeeded);
        Assert.False((await Workflow(d).RejectAsync(id, "Duplicate", Reporter(d))).Succeeded);
    }

    [Fact]
    public async Task Reject_needs_a_reason_and_only_works_on_submitted_issues()
    {
        await using var d = await IssueTestData.CreateAsync();
        var submitted = await d.ReportAsync();
        var assigned = await AssignedToItAsync(d);

        var noReason = await Workflow(d).RejectAsync(submitted, "  ", Admin(d));
        var afterAssign = await Workflow(d).RejectAsync(assigned, "Duplicate", Admin(d));
        var ok = await Workflow(d).RejectAsync(submitted, "Duplicate of ISS-000002", Admin(d));

        Assert.False(noReason.Succeeded);
        Assert.False(afterAssign.Succeeded);
        AssertOk(ok);
        Assert.Equal(IssueStatus.Rejected, (await ReloadAsync(d, submitted)).Status);
    }

    [Fact]
    public async Task Admin_can_reassign_work_in_progress_to_another_team()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await InProgressAsync(d);

        AssertOk(await Workflow(d).AssignAsync(id, IssuePriority.Critical, d.ElectricalTeam.Id, null, "Power problem, not the projector", Admin(d)));

        var issue = await ReloadAsync(d, id);
        Assert.Equal(IssueStatus.Assigned, issue.Status);
        Assert.Equal(d.ElectricalTeam.Id, issue.AssignedTeamId);
        Assert.Null(issue.AssignedStaffId);
        Assert.Equal(IssuePriority.Critical, issue.Priority);
    }

    // ---------- Support staff ----------

    [Fact]
    public async Task Staff_of_another_team_cannot_take_or_start()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await AssignedToItAsync(d);

        Assert.False((await Workflow(d).TakeAsync(id, Elec(d))).Succeeded);
        Assert.False((await Workflow(d).StartAsync(id, Elec(d))).Succeeded);
    }

    [Fact]
    public async Task Take_twice_is_refused()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await AssignedToItAsync(d);
        AssertOk(await Workflow(d).TakeAsync(id, It(d)));

        Assert.False((await Workflow(d).TakeAsync(id, It(d))).Succeeded);
    }

    [Fact]
    public async Task Start_by_team_member_sets_them_as_technician()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await AssignedToItAsync(d);

        AssertOk(await Workflow(d).StartAsync(id, It(d)));

        var issue = await ReloadAsync(d, id);
        Assert.Equal(IssueStatus.InProgress, issue.Status);
        Assert.Equal(d.ItStaff.Id, issue.AssignedStaffId);
    }

    [Fact]
    public async Task Resolve_requires_a_resolution_note()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await InProgressAsync(d);

        var result = await Workflow(d).ResolveAsync(id, "done", It(d)); // too short

        Assert.Equal("Note", result.Field);
        Assert.Equal(IssueStatus.InProgress, (await ReloadAsync(d, id)).Status);
    }

    [Fact]
    public async Task Hold_requires_a_reason()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await InProgressAsync(d);

        Assert.False((await Workflow(d).HoldAsync(id, "", It(d))).Succeeded);
    }

    [Theory]
    [InlineData("assigned")]
    [InlineData("submitted")]
    public async Task Cannot_resolve_before_work_has_started(string state)
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = state == "assigned" ? await AssignedToItAsync(d) : await d.ReportAsync();

        var result = await Workflow(d).ResolveAsync(id, "Replaced the projector lamp.", Admin(d));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Reporter_and_other_team_cannot_do_staff_work()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await InProgressAsync(d);

        Assert.False((await Workflow(d).ResolveAsync(id, "I fixed it myself somehow", Reporter(d))).Succeeded);
        Assert.False((await Workflow(d).ResolveAsync(id, "Replaced the projector lamp.", Elec(d))).Succeeded);
        Assert.False((await Workflow(d).HoldAsync(id, "Waiting", Reporter(d))).Succeeded);
    }

    // ---------- Reporter: close / reopen ----------

    [Fact]
    public async Task Only_reporter_or_admin_can_close()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await ResolvedAsync(d);

        Assert.False((await Workflow(d).CloseAsync(id, Other(d))).Succeeded);
        Assert.False((await Workflow(d).CloseAsync(id, It(d))).Succeeded);
        AssertOk(await Workflow(d).CloseAsync(id, Admin(d)));
    }

    [Fact]
    public async Task Reopen_within_window_goes_back_to_the_same_team()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await ResolvedAsync(d);

        AssertOk(await Workflow(d).ReopenAsync(id, "The projector turns off again after 10 minutes", Reporter(d)));

        var issue = await ReloadAsync(d, id);
        Assert.Equal(IssueStatus.Assigned, issue.Status);
        Assert.Equal(1, issue.ReopenCount);
        Assert.Equal(d.ItTeam.Id, issue.AssignedTeamId);
        Assert.Equal(d.ItStaff.Id, issue.AssignedStaffId);
        Assert.Null(issue.ResolvedAt);
        Assert.Null(issue.ResolutionNote);
    }

    [Fact]
    public async Task Reopen_after_the_window_is_refused()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await ResolvedAsync(d);
        var db = d.Services.Get<ApplicationDbContext>();
        var issue = await db.Issues.SingleAsync(i => i.Id == id);
        issue.ResolvedAt = DateTime.UtcNow.AddDays(-8); // window is 7 days
        await db.SaveChangesAsync();

        var result = await Workflow(d).ReopenAsync(id, "Still broken", Reporter(d));

        Assert.False(result.Succeeded);
        Assert.Equal(IssueStatus.Resolved, (await ReloadAsync(d, id)).Status);
    }

    [Fact]
    public async Task Only_the_reporter_can_reopen_and_a_reason_is_needed()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await ResolvedAsync(d);

        Assert.False((await Workflow(d).ReopenAsync(id, "Still broken", Admin(d))).Succeeded);
        Assert.False((await Workflow(d).ReopenAsync(id, "Still broken", Other(d))).Succeeded);
        Assert.False((await Workflow(d).ReopenAsync(id, "", Reporter(d))).Succeeded);
    }

    // ---------- Final states ----------

    [Fact]
    public async Task Closed_issue_cannot_be_reassigned_or_reopened()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await ResolvedAsync(d);
        AssertOk(await Workflow(d).CloseAsync(id, Reporter(d)));

        Assert.False((await Workflow(d).AssignAsync(id, IssuePriority.Low, d.ItTeam.Id, null, null, Admin(d))).Succeeded);
        Assert.False((await Workflow(d).ReopenAsync(id, "Broken again", Reporter(d))).Succeeded);
    }
}
