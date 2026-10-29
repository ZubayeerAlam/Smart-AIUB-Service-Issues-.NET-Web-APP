using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Controllers;

public class DepartmentsController : AdminControllerBase
{
    private readonly IDepartmentService _departments;

    public DepartmentsController(IDepartmentService departments)
    {
        _departments = departments;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var model = new ListPageViewModel<Department>
        {
            Search = search,
            Status = status,
            Results = await _departments.GetPageAsync(search, status, page)
        };
        return View(model);
    }

    public IActionResult Create() => View("Form", new DepartmentFormViewModel());

    [HttpPost]
    public Task<IActionResult> Create(DepartmentFormViewModel model)
    {
        model.Id = 0; // never let a posted Id turn "create" into "update"
        return SaveAsync(model, "added");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _departments.GetFormAsync(id);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    public Task<IActionResult> Edit(int id, DepartmentFormViewModel model)
    {
        model.Id = id;
        return SaveAsync(model, "updated");
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, string? returnUrl)
    {
        var result = await _departments.SetActiveAsync(id, isActive);
        Flash(result, isActive ? "Department activated." : "Department deactivated.");
        return RedirectBack(returnUrl);
    }

    private async Task<IActionResult> SaveAsync(DepartmentFormViewModel model, string verb)
    {
        if (ModelState.IsValid)
        {
            var result = await _departments.SaveAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Department '{model.Name.Trim()}' {verb}.";
                return RedirectToAction(nameof(Index));
            }
            AddError(result);
        }
        return View("Form", model);
    }
}
