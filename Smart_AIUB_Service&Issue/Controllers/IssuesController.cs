using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Assets;
using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.Controllers;

/// <summary>
/// Reporting and viewing issues. Any logged-in user may report; access to a single
/// issue is decided per issue by IssueAuthorizationHandler (resource-based authorization).
/// </summary>
[Authorize]
public class IssuesController : Controller
{
    private readonly IIssueService _issues;
    private readonly IIssueWorkflowService _workflow;
    private readonly IIssueActorService _actors;
    private readonly IAuthorizationService _authorization;
    private readonly UserManager<ApplicationUser> _userManager;

    public IssuesController(
        IIssueService issues,
        IIssueWorkflowService workflow,
        IIssueActorService actors,
        IAuthorizationService authorization,
        UserManager<ApplicationUser> userManager)
    {
        _issues = issues;
        _workflow = workflow;
        _actors = actors;
        _authorization = authorization;
        _userManager = userManager;
    }

    // ---------- Lists ----------

    // [FromQuery] IssueListFilter: ASP.NET Core fills the object from the URL
    // (?search=...&status=...&buildingId=...&from=2026-10-01), one property per query value.

    /// <summary>My issues.</summary>
    public async Task<IActionResult> Index([FromQuery] IssueListFilter filter, int page = 1)
    {
        var model = IssueListViewModel.FromFilter(filter);
        model.Results = await _issues.GetMyIssuesAsync(_userManager.GetUserId(User)!, filter, page);
        await _issues.FillListOptionsAsync(model);
        return View(model);
    }

    /// <summary>Everyone's open issues (to avoid duplicate reports). No reporter names.</summary>
    public async Task<IActionResult> Open([FromQuery] IssueListFilter filter, int page = 1)
    {
        var model = IssueListViewModel.FromFilter(filter);
        model.Results = await _issues.GetOpenIssuesAsync(filter, page);
        await _issues.FillListOptionsAsync(model);
        return View(model);
    }

    // ---------- Report ----------

    public async Task<IActionResult> Report()
    {
        var model = new IssueFormViewModel();
        await _issues.FillFormOptionsAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    public async Task<IActionResult> Report(IssueFormViewModel model)
    {
        model.Id = 0;
        if (ModelState.IsValid)
        {
            var result = await _issues.ReportAsync(model, _userManager.GetUserId(User)!);
            if (result.Succeeded)
            {
                TempData["Success"] = "Thank you! Your issue was reported. You can follow its progress here.";
                return RedirectToAction(nameof(Details), new { id = result.Value });
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }

        await _issues.FillFormOptionsAsync(model);
        return View("Form", model);
    }

    /// <summary>JSON for the cascading Building → Room dropdown.</summary>
    [HttpGet]
    public async Task<IActionResult> Rooms(int buildingId) =>
        Json(await _issues.GetRoomOptionsAsync(buildingId));

    // ---------- Details ----------

    public async Task<IActionResult> Details(int id)
    {
        var issue = await _issues.FindAsync(id);
        if (issue is null || !await IsAllowedAsync(issue, IssueOperations.View))
        {
            // 404 (not 403) so nobody can even tell whether another user's issue exists.
            return NotFound();
        }

        var model = await _issues.GetDetailsAsync(id, await _actors.GetAsync(User));
        return model is null ? NotFound() : View(model);
    }

    // ---------- Edit / cancel (only while Submitted) ----------

    public async Task<IActionResult> Edit(int id)
    {
        var issue = await _issues.FindAsync(id);
        if (issue is null || !await IsAllowedAsync(issue, IssueOperations.View))
        {
            return NotFound();
        }
        if (!await IsAllowedAsync(issue, IssueOperations.Edit))
        {
            TempData["Error"] = "This issue can no longer be edited because it has already been reviewed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = await _issues.GetEditFormAsync(id);
        await _issues.FillFormOptionsAsync(model!);
        return View("Form", model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, IssueFormViewModel model)
    {
        model.Id = id;
        var issue = await _issues.FindAsync(id);
        if (issue is null || !await IsAllowedAsync(issue, IssueOperations.View))
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var result = await _issues.UpdateAsync(model, await _actors.GetAsync(User));
            if (result.Succeeded)
            {
                TempData["Success"] = "Issue updated.";
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }

        await _issues.FillFormOptionsAsync(model);
        return View("Form", model);
    }

    // ---------- Support staff: team queue ----------

    /// <summary>Issues of the logged-in technician's team.</summary>
    [Authorize(Roles = Roles.SupportStaff)]
    public async Task<IActionResult> Team(string? scope, string? search, IssueStatus? status, IssuePriority? priority, int page = 1)
    {
        var filter = new IssueListFilter { Search = search, Status = status, Priority = priority };
        var model = new IssueListViewModel
        {
            Scope = scope,
            Search = search,
            Status = status,
            Priority = priority,
            Results = await _issues.GetTeamIssuesAsync(await _actors.GetAsync(User), scope, filter, page)
        };
        return View(model);
    }

    // ---------- Workflow actions ----------
    // Each one: 404 if the user may not even see the issue, then the workflow service
    // decides whether this transition is allowed for this user right now.

    [HttpPost]
    public Task<IActionResult> Cancel(int id, IssueNoteViewModel model) =>
        RunAsync(id, actor => _workflow.CancelAsync(id, model.Note, actor), "Issue cancelled.");

    [HttpPost]
    public Task<IActionResult> Take(int id) =>
        RunAsync(id, actor => _workflow.TakeAsync(id, actor), "You took this issue. Press \"Start work\" when you begin.");

    [HttpPost]
    public Task<IActionResult> Start(int id) =>
        RunAsync(id, actor => _workflow.StartAsync(id, actor), "Work started.");

    [HttpPost]
    public Task<IActionResult> Hold(int id, IssueNoteViewModel model) =>
        RunAsync(id, actor => _workflow.HoldAsync(id, model.Note, actor), "Issue put on hold.");

    [HttpPost]
    public Task<IActionResult> Resume(int id) =>
        RunAsync(id, actor => _workflow.ResumeAsync(id, actor), "Work resumed.");

    [HttpPost]
    public Task<IActionResult> Resolve(int id, IssueNoteViewModel model) =>
        RunAsync(id, actor => _workflow.ResolveAsync(id, model.Note, actor), "Issue marked as resolved. The reporter can now confirm the fix.");

    [HttpPost]
    public Task<IActionResult> Close(int id) =>
        RunAsync(id, actor => _workflow.CloseAsync(id, actor), "Thanks for confirming! The issue is closed.");

    [HttpPost]
    public Task<IActionResult> Reopen(int id, IssueNoteViewModel model) =>
        RunAsync(id, actor => _workflow.ReopenAsync(id, model.Note, actor), "Issue reopened and sent back to the support team.");

    [HttpPost]
    public Task<IActionResult> LinkAsset(int id, LinkAssetViewModel model) =>
        RunAsync(id, actor => _issues.LinkAssetAsync(id, model.AssetId, actor),
            model.AssetId is null ? "Asset link removed." : "Asset linked to this issue.");

    [HttpPost]
    public Task<IActionResult> Comment(int id, AddCommentViewModel model) =>
        RunAsync(id, actor => _issues.AddCommentAsync(id, model, actor), model.IsInternal ? "Internal note added." : "Comment added.");

    private async Task<IActionResult> RunAsync(int id, Func<IssueActor, Task<ServiceResult>> action, string successMessage)
    {
        var issue = await _issues.FindAsync(id);
        if (issue is null || !await IsAllowedAsync(issue, IssueOperations.View))
        {
            return NotFound();
        }

        var result = await action(await _actors.GetAsync(User));
        if (result.Succeeded)
        {
            TempData["Success"] = successMessage;
        }
        else
        {
            TempData["Error"] = result.Error;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<bool> IsAllowedAsync(Issue issue, OperationAuthorizationRequirement operation) =>
        (await _authorization.AuthorizeAsync(User, issue, operation)).Succeeded;
}
