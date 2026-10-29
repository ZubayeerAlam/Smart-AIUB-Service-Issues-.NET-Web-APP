using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Assets;

namespace Smart_AIUB_Service_Issue.Controllers;

/// <summary>Support staff and admins log the work they did on an asset.</summary>
[Authorize(Policy = Policies.StaffOrAdmin)]
public class MaintenanceController : Controller
{
    private readonly IMaintenanceService _maintenance;
    private readonly IIssueActorService _actors;

    public MaintenanceController(IMaintenanceService maintenance, IIssueActorService actors)
    {
        _maintenance = maintenance;
        _actors = actors;
    }

    public async Task<IActionResult> Create(int assetId, int? issueId)
    {
        var model = await _maintenance.NewFormAsync(assetId, issueId);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(MaintenanceFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _maintenance.CreateAsync(model, await _actors.GetAsync(User));
            if (result.Succeeded)
            {
                TempData["Success"] = "Maintenance record saved.";
                return RedirectToAction("Details", "Assets", new { id = model.AssetId });
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }

        await _maintenance.FillFormOptionsAsync(model);
        return View(model);
    }
}
