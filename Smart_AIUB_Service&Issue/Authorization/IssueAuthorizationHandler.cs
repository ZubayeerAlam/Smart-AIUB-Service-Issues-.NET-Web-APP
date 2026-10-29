using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;

namespace Smart_AIUB_Service_Issue.Authorization;

/// <summary>Operations that can be checked against one specific issue.</summary>
public static class IssueOperations
{
    public static readonly OperationAuthorizationRequirement View = new() { Name = nameof(View) };
    public static readonly OperationAuthorizationRequirement Edit = new() { Name = nameof(Edit) };
}

/// <summary>
/// Resource-based authorization: [Authorize] can only check roles, but "is this MY issue?"
/// needs the issue itself. Controllers call
///   await _authorization.AuthorizeAsync(User, issue, IssueOperations.View)
/// and this handler answers using IssuePermissions. This is what stops a user from
/// changing the id in the URL to open someone else's issue (IDOR).
/// </summary>
public class IssueAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Issue>
{
    private readonly IIssueActorService _actors;

    public IssueAuthorizationHandler(IIssueActorService actors)
    {
        _actors = actors;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, Issue issue)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var actor = await _actors.GetAsync(context.User);
        var allowed = requirement.Name switch
        {
            nameof(IssueOperations.View) => IssuePermissions.CanView(issue, actor),
            nameof(IssueOperations.Edit) => IssuePermissions.CanEdit(issue, actor),
            _ => false
        };

        if (allowed)
        {
            context.Succeed(requirement);
        }
    }
}
