using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
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

    /// <summary>My issues.</summary>
    public async Task<IActionResult> Index(string? search, IssueStatus? status, int page = 1)
    {
        var model = new IssueListViewModel
        {
            Search = search,
            Status = status,
            Results = await _issues.GetMyIssuesAsync(_userManager.GetUserId(User)!, search, status, page)
        };
        return View(model);
    }

    /// <summary>Everyone's open issues (to avoid duplicate reports). No reporter names.</summary>
    public async Task<IActionResult> Open(string? search, IssueStatus? status, int page = 1)
    {
        var model = new IssueListViewModel
        {
            Search = search,
            Status = status,
            Results = await _issues.GetOpenIssuesAsync(search, status, page)
        };
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

    [HttpPost]
    public async Task<IActionResult> Cancel(int id, CancelIssueViewModel model)
    {
        var issue = await _issues.FindAsync(id);
        if (issue is null || !await IsAllowedAsync(issue, IssueOperations.View))
        {
            return NotFound();
        }

        var result = ModelState.IsValid
            ? await _workflow.CancelAsync(id, model.Reason, await _actors.GetAsync(User))
            : ServiceResult.Fail("The reason is too long (max 500 characters).");

        if (result.Succeeded)
        {
            TempData["Success"] = "Issue cancelled.";
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
