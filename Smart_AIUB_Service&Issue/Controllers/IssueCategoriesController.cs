using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Controllers;

public class IssueCategoriesController : AdminControllerBase
{
    private readonly IIssueCategoryService _categories;
    private readonly ISupportTeamService _teams;

    public IssueCategoriesController(IIssueCategoryService categories, ISupportTeamService teams)
    {
        _categories = categories;
        _teams = teams;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var model = new ListPageViewModel<IssueCategoryListItem>
        {
            Search = search,
            Status = status,
            Results = await _categories.GetPageAsync(search, status, page)
        };
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = new IssueCategoryFormViewModel { TeamOptions = await _teams.GetOptionsAsync() };
        return View("Form", model);
    }

    [HttpPost]
    public Task<IActionResult> Create(IssueCategoryFormViewModel model)
    {
        model.Id = 0;
        return SaveAsync(model, "added");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _categories.GetFormAsync(id);
        if (model is null)
        {
            return NotFound();
        }
        model.TeamOptions = await _teams.GetOptionsAsync(model.DefaultSupportTeamId);
        return View("Form", model);
    }

    [HttpPost]
    public Task<IActionResult> Edit(int id, IssueCategoryFormViewModel model)
    {
        model.Id = id;
        return SaveAsync(model, "updated");
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, string? returnUrl)
    {
        var result = await _categories.SetActiveAsync(id, isActive);
        Flash(result, isActive ? "Category activated." : "Category deactivated. It can no longer be chosen for new issues.");
        return RedirectBack(returnUrl);
    }

    private async Task<IActionResult> SaveAsync(IssueCategoryFormViewModel model, string verb)
    {
        if (ModelState.IsValid)
        {
            var result = await _categories.SaveAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Category '{model.Name.Trim()}' {verb}.";
                return RedirectToAction(nameof(Index));
            }
            AddError(result);
        }
        model.TeamOptions = await _teams.GetOptionsAsync(model.DefaultSupportTeamId);
        return View("Form", model);
    }
}
