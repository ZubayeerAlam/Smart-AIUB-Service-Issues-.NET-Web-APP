using Microsoft.AspNetCore.Authorization;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;

namespace Smart_AIUB_Service_Issue.Tests.Authorization;

/// <summary>
/// Resource-based authorization through the real IAuthorizationService + IssueAuthorizationHandler,
/// exactly as the controller uses it. These are the IDOR tests: changing the id in the URL must
/// not give access to someone else's issue.
/// </summary>
public class IssueAuthorizationTests
{
    private static async Task<bool> CanAsync(IssueTestData data, ApplicationUser user, string role, int issueId,
        Microsoft.AspNetCore.Authorization.Infrastructure.OperationAuthorizationRequirement operation)
    {
        var issue = await data.Services.Get<IIssueService>().FindAsync(issueId);
        var result = await data.Services.Get<IAuthorizationService>()
            .AuthorizeAsync(IssueTestData.PrincipalFor(user, role), issue!, operation);
        return result.Succeeded;
    }

    [Fact]
    public async Task Reporter_can_view_own_issue()
    {
        await using var data = await IssueTestData.CreateAsync();
        var id = await data.ReportAsync();

        Assert.True(await CanAsync(data, data.Reporter, Roles.Student, id, IssueOperations.View));
    }

    [Fact]
    public async Task Other_reporter_cannot_view_someone_elses_issue()
    {
        await using var data = await IssueTestData.CreateAsync();
        var id = await data.ReportAsync();

        Assert.False(await CanAsync(data, data.OtherReporter, Roles.Faculty, id, IssueOperations.View));
        Assert.False(await CanAsync(data, data.OtherReporter, Roles.Faculty, id, IssueOperations.Edit));
    }

    [Fact]
    public async Task Staff_can_view_only_issues_of_their_own_team()
    {
        await using var data = await IssueTestData.CreateAsync();
        var id = await data.ReportAsync();

        // Not assigned yet: no staff can see it.
        Assert.False(await CanAsync(data, data.ItStaff, Roles.SupportStaff, id, IssueOperations.View));

        await data.SetStateAsync(id, IssueStatus.Assigned, data.ItTeam);

        Assert.True(await CanAsync(data, data.ItStaff, Roles.SupportStaff, id, IssueOperations.View));
        Assert.False(await CanAsync(data, data.ElectricalStaff, Roles.SupportStaff, id, IssueOperations.View));
    }

    [Fact]
    public async Task Admin_can_view_any_issue()
    {
        await using var data = await IssueTestData.CreateAsync();
        var id = await data.ReportAsync();

        Assert.True(await CanAsync(data, data.Admin, Roles.Admin, id, IssueOperations.View));
    }

    [Fact]
    public async Task Reporter_can_edit_only_while_submitted()
    {
        await using var data = await IssueTestData.CreateAsync();
        var id = await data.ReportAsync();

        Assert.True(await CanAsync(data, data.Reporter, Roles.Student, id, IssueOperations.Edit));

        await data.SetStateAsync(id, IssueStatus.Assigned, data.ItTeam);

        Assert.False(await CanAsync(data, data.Reporter, Roles.Student, id, IssueOperations.Edit));
    }
}
