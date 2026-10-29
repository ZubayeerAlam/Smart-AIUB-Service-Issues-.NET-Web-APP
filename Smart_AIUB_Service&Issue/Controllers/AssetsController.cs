using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Assets;

namespace Smart_AIUB_Service_Issue.Controllers;

/// <summary>
/// Asset registry. Support staff and admins can browse it (StaffOrAdmin on the class);
/// only admins can add or edit (AdminOnly on those actions; both policies must pass).
/// </summary>
[Authorize(Policy = Policies.StaffOrAdmin)]
public class AssetsController : Controller
{
    private readonly IAssetService _assets;
    private readonly IAssetTypeService _types;
    private readonly IBuildingService _buildings;

    public AssetsController(IAssetService assets, IAssetTypeService types, IBuildingService buildings)
    {
        _assets = assets;
        _types = types;
        _buildings = buildings;
    }

    public async Task<IActionResult> Index(string? search, int? typeId, int? buildingId, AssetStatus? status, int page = 1)
    {
        var model = new AssetIndexViewModel
        {
            Search = search,
            TypeId = typeId,
            BuildingId = buildingId,
            Status = status,
            TypeOptions = await _types.GetOptionsAsync(typeId),
            BuildingOptions = await _buildings.GetOptionsAsync(buildingId),
            Results = await _assets.GetPageAsync(search, typeId, buildingId, status, page)
        };
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await _assets.GetDetailsAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Create()
    {
        var model = new AssetFormViewModel();
        await _assets.FillFormOptionsAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public Task<IActionResult> Create(AssetFormViewModel model)
    {
        model.Id = 0;
        return SaveAsync(model, "added");
    }

    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _assets.GetFormAsync(id);
        if (model is null)
        {
            return NotFound();
        }
        await _assets.FillFormOptionsAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public Task<IActionResult> Edit(int id, AssetFormViewModel model)
    {
        model.Id = id;
        return SaveAsync(model, "updated");
    }

    private async Task<IActionResult> SaveAsync(AssetFormViewModel model, string verb)
    {
        if (ModelState.IsValid)
        {
            var result = await _assets.SaveAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Asset {model.AssetTag.Trim().ToUpperInvariant()} {verb}.";
                return RedirectToAction(nameof(Details), new { id = result.Value });
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }

        await _assets.FillFormOptionsAsync(model);
        return View("Form", model);
    }
}
