using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Controllers;

public class BuildingsController : AdminControllerBase
{
    private readonly IBuildingService _buildings;

    public BuildingsController(IBuildingService buildings)
    {
        _buildings = buildings;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var model = new ListPageViewModel<BuildingListItem>
        {
            Search = search,
            Status = status,
            Results = await _buildings.GetPageAsync(search, status, page)
        };
        return View(model);
    }

    public IActionResult Create() => View("Form", new BuildingFormViewModel());

    [HttpPost]
    public Task<IActionResult> Create(BuildingFormViewModel model)
    {
        model.Id = 0;
        return SaveAsync(model, "added");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _buildings.GetFormAsync(id);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    public Task<IActionResult> Edit(int id, BuildingFormViewModel model)
    {
        model.Id = id;
        return SaveAsync(model, "updated");
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, string? returnUrl)
    {
        var result = await _buildings.SetActiveAsync(id, isActive);
        Flash(result, isActive ? "Building activated." : "Building deactivated. Its rooms can no longer be chosen for new issues.");
        return RedirectBack(returnUrl);
    }

    private async Task<IActionResult> SaveAsync(BuildingFormViewModel model, string verb)
    {
        if (ModelState.IsValid)
        {
            var result = await _buildings.SaveAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Building '{model.Name.Trim()}' {verb}.";
                return RedirectToAction(nameof(Index));
            }
            AddError(result);
        }
        return View("Form", model);
    }
}
