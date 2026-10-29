using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Services.Interfaces;

namespace Smart_AIUB_Service_Issue.Services;

public class IssueActorService : IIssueActorService
{
    private readonly ApplicationDbContext _db;
    private IssueActor? _cached; // one lookup per request is enough

    public IssueActorService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IssueActor> GetAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("No logged-in user.");

        if (_cached is not null && _cached.UserId == userId)
        {
            return _cached;
        }

        var isStaff = user.IsInRole(Roles.SupportStaff);

        // The team is read from the database (not the cookie), so a team change applies immediately.
        int? teamId = isStaff
            ? await _db.Users.Where(u => u.Id == userId).Select(u => u.SupportTeamId).FirstOrDefaultAsync()
            : null;

        _cached = new IssueActor(userId, user.IsInRole(Roles.Admin), isStaff, teamId);
        return _cached;
    }
}
