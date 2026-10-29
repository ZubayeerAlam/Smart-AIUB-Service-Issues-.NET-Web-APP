using System.Security.Claims;
using Smart_AIUB_Service_Issue.Authorization;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IIssueActorService
{
    /// <summary>Builds the IssueActor (id, role, support team) for the logged-in user.</summary>
    Task<IssueActor> GetAsync(ClaimsPrincipal user);
}
