using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Services.Interfaces;

namespace Smart_AIUB_Service_Issue.Controllers;

/// <summary>One URL (/Dashboard) that shows the right dashboard for the user's role.</summary>
[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboards;
    private readonly IIssueActorService _actors;

    public DashboardController(IDashboardService dashboards, IIssueActorService actors)
    {
        _dashboards = dashboards;
        _actors = actors;
    }

    public async Task<IActionResult> Index()
    {
        var actor = await _actors.GetAsync(User);

        if (actor.IsAdmin)
        {
            return View("Admin", await _dashboards.GetAdminAsync());
        }
        if (actor.IsStaff)
        {
            return View("Staff", await _dashboards.GetStaffAsync(actor));
        }
        return View("Reporter", await _dashboards.GetReporterAsync(actor.UserId));
    }
}
