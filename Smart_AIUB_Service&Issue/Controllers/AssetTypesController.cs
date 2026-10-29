using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;
using Smart_AIUB_Service_Issue.ViewModels.Assets;

namespace Smart_AIUB_Service_Issue.Controllers;

public class AssetTypesController : AdminControllerBase
{
    private readonly IAssetTypeService _types;

    public AssetTypesController(IAssetTypeService types)
    {
        _types = types;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var model = new ListPageViewModel<AssetTypeListItem>
        {
            Search = search,
            Status = status,
            Results = await _types.GetPageAsync(search, status, page)
        };
        return View(model);
    }

    public IActionResult Create() => View("Form", new AssetTypeFormViewModel());

    [HttpPost]
    public Task<IActionResult> Create(AssetTypeFormViewModel model)
    {
        model.Id = 0;
        return SaveAsync(model, "added");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _types.GetFormAsync(id);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    public Task<IActionResult> Edit(int id, AssetTypeFormViewModel model)
    {
        model.Id = id;
        return SaveAsync(model, "updated");
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, string? returnUrl)
    {
        var result = await _types.SetActiveAsync(id, isActive);
        Flash(result, isActive ? "Asset type activated." : "Asset type deactivated. It can no longer be chosen for new assets.");
        return RedirectBack(returnUrl);
    }

    private async Task<IActionResult> SaveAsync(AssetTypeFormViewModel model, string verb)
    {
        if (ModelState.IsValid)
        {
            var result = await _types.SaveAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Asset type '{model.Name.Trim()}' {verb}.";
                return RedirectToAction(nameof(Index));
            }
            AddError(result);
        }
        return View("Form", model);
    }
}
