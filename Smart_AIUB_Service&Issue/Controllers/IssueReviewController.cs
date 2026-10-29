using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.Controllers;

/// <summary>Admin: review queue, all issues, assign/reassign and reject.</summary>
public class IssueReviewController : AdminControllerBase
{
    private readonly IIssueService _issues;
    private readonly IIssueWorkflowService _workflow;
    private readonly IIssueActorService _actors;
    private readonly ISupportTeamService _teams;

    public IssueReviewController(
        IIssueService issues, IIssueWorkflowService workflow, IIssueActorService actors, ISupportTeamService teams)
    {
        _issues = issues;
        _workflow = workflow;
        _actors = actors;
        _teams = teams;
    }

    /// <summary>Submitted issues waiting for review, oldest first.</summary>
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var model = new IssueListViewModel
        {
            Search = search,
            Results = await _issues.GetReviewQueueAsync(search, page)
        };
        return View(model);
    }

    /// <summary>Every issue, with all filters (status, priority, team, category, building, date range).</summary>
    public async Task<IActionResult> All([FromQuery] IssueListFilter filter, int page = 1)
    {
        var model = IssueListViewModel.FromFilter(filter);
        model.Results = await _issues.GetAllIssuesAsync(filter, page);
        model.TeamOptions = await _teams.GetOptionsAsync(filter.TeamId);
        await _issues.FillListOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Assign(int id, AssignIssueViewModel model)
    {
        var result = await _workflow.AssignAsync(
            id, model.Priority, model.TeamId, model.StaffId, model.Note, await _actors.GetAsync(User));
        Flash(result, "Issue assigned. The support team can see it now.");
        return RedirectToAction("Details", "Issues", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id, IssueNoteViewModel model)
    {
        var result = await _workflow.RejectAsync(id, model.Note, await _actors.GetAsync(User));
        Flash(result, "Issue rejected. The reporter can see the reason.");
        return RedirectToAction("Details", "Issues", new { id });
    }
}
