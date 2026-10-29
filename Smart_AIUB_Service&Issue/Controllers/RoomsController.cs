using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Controllers;

public class RoomsController : AdminControllerBase
{
    private readonly IRoomService _rooms;
    private readonly IBuildingService _buildings;

    public RoomsController(IRoomService rooms, IBuildingService buildings)
    {
        _rooms = rooms;
        _buildings = buildings;
    }

    public async Task<IActionResult> Index(string? search, string? status, int? buildingId, int page = 1)
    {
        var model = new RoomIndexViewModel
        {
            Search = search,
            Status = status,
            BuildingId = buildingId,
            BuildingOptions = await _buildings.GetOptionsAsync(buildingId),
            Results = await _rooms.GetPageAsync(search, status, buildingId, page)
        };
        return View(model);
    }

    /// <summary>"Add room" can be opened from a filtered list with the building pre-selected.</summary>
    public async Task<IActionResult> Create(int? buildingId)
    {
        var model = new RoomFormViewModel { BuildingId = buildingId, Floor = 1 };
        model.BuildingOptions = await _buildings.GetOptionsAsync();
        return View("Form", model);
    }

    [HttpPost]
    public Task<IActionResult> Create(RoomFormViewModel model)
    {
        model.Id = 0;
        return SaveAsync(model, "added");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _rooms.GetFormAsync(id);
        if (model is null)
        {
            return NotFound();
        }
        model.BuildingOptions = await _buildings.GetOptionsAsync(model.BuildingId);
        return View("Form", model);
    }

    [HttpPost]
    public Task<IActionResult> Edit(int id, RoomFormViewModel model)
    {
        model.Id = id;
        return SaveAsync(model, "updated");
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, string? returnUrl)
    {
        var result = await _rooms.SetActiveAsync(id, isActive);
        Flash(result, isActive ? "Room activated." : "Room deactivated.");
        return RedirectBack(returnUrl);
    }

    private async Task<IActionResult> SaveAsync(RoomFormViewModel model, string verb)
    {
        if (ModelState.IsValid)
        {
            var result = await _rooms.SaveAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Room {model.RoomNumber.Trim().ToUpperInvariant()} {verb}.";
                return RedirectToAction(nameof(Index), new { buildingId = model.BuildingId });
            }
            AddError(result);
        }
        model.BuildingOptions = await _buildings.GetOptionsAsync(model.BuildingId);
        return View("Form", model);
    }
}
