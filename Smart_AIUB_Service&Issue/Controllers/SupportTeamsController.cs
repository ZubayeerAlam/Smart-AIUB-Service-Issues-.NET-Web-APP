using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Controllers;

public class SupportTeamsController : AdminControllerBase
{
    private readonly ISupportTeamService _teams;

    public SupportTeamsController(ISupportTeamService teams)
    {
        _teams = teams;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var model = new ListPageViewModel<SupportTeamListItem>
        {
            Search = search,
            Status = status,
            Results = await _teams.GetPageAsync(search, status, page)
        };
        return View(model);
    }

    public IActionResult Create() => View("Form", new SupportTeamFormViewModel());

    [HttpPost]
    public Task<IActionResult> Create(SupportTeamFormViewModel model)
    {
        model.Id = 0;
        return SaveAsync(model, "added");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _teams.GetFormAsync(id);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    public Task<IActionResult> Edit(int id, SupportTeamFormViewModel model)
    {
        model.Id = id;
        return SaveAsync(model, "updated");
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, string? returnUrl)
    {
        var result = await _teams.SetActiveAsync(id, isActive);
        Flash(result, isActive ? "Team activated." : "Team deactivated. It can no longer receive new issues.");
        return RedirectBack(returnUrl);
    }

    private async Task<IActionResult> SaveAsync(SupportTeamFormViewModel model, string verb)
    {
        if (ModelState.IsValid)
        {
            var result = await _teams.SaveAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Team '{model.Name.Trim()}' {verb}.";
                return RedirectToAction(nameof(Index));
            }
            AddError(result);
        }
        return View("Form", model);
    }
}
