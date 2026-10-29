using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Controllers;

public class UsersController : AdminControllerBase
{
    private readonly IUserManagementService _users;
    private readonly IDepartmentService _departments;
    private readonly ISupportTeamService _teams;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(
        IUserManagementService users,
        IDepartmentService departments,
        ISupportTeamService teams,
        UserManager<ApplicationUser> userManager)
    {
        _users = users;
        _departments = departments;
        _teams = teams;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, string? status, string? role, int page = 1)
    {
        var model = new UserIndexViewModel
        {
            Search = search,
            Status = status,
            Role = role,
            CurrentUserId = _userManager.GetUserId(User),
            Results = await _users.GetPageAsync(search, status, role, page)
        };
        return View(model);
    }

    // ---------- Create staff / admin ----------

    public async Task<IActionResult> Create()
    {
        var model = new CreateUserViewModel();
        await FillCreateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _users.CreateAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = $"{model.Role} account created for {model.Email.Trim()}. Share the temporary password with them privately.";
                return RedirectToAction(nameof(Index));
            }
            AddError(result);
        }

        await FillCreateOptionsAsync(model);
        return View(model);
    }

    // ---------- Edit profile / team ----------

    public async Task<IActionResult> Edit(string id)
    {
        var model = await _users.GetEditAsync(id);
        if (model is null)
        {
            return NotFound();
        }
        await FillEditOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            var result = await _users.UpdateAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = $"{model.FullName.Trim()} updated.";
                return RedirectToAction(nameof(Index));
            }
            AddError(result);
        }

        // Reload the read-only bits (email, role) from the database for redisplay.
        var saved = await _users.GetEditAsync(id);
        if (saved is null)
        {
            return NotFound();
        }
        model.Email = saved.Email;
        model.Role = saved.Role;
        await FillEditOptionsAsync(model);
        return View(model);
    }

    // ---------- Activate / deactivate ----------

    [HttpPost]
    public async Task<IActionResult> SetActive(string id, bool isActive, string? returnUrl)
    {
        var result = await _users.SetActiveAsync(id, isActive, _userManager.GetUserId(User)!);
        Flash(result, isActive
            ? "User activated."
            : "User deactivated. They will be signed out within about 3 minutes.");
        return RedirectBack(returnUrl);
    }

    // ---------- Reset password ----------

    public async Task<IActionResult> ResetPassword(string id)
    {
        var model = await _users.GetResetPasswordAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string id, ResetPasswordViewModel model)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            var result = await _users.ResetPasswordAsync(id, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["Success"] = "Password reset. Any lockout was cleared. Share the new password with the user privately.";
                return RedirectToAction(nameof(Index));
            }
            AddError(result);
        }

        var saved = await _users.GetResetPasswordAsync(id);
        if (saved is null)
        {
            return NotFound();
        }
        model.DisplayName = saved.DisplayName;
        return View(model);
    }

    private async Task FillCreateOptionsAsync(CreateUserViewModel model)
    {
        model.RoleOptions = UserManagementService.CreatableRoles.Select(r => new SelectListItem(r, r));
        model.TeamOptions = await _teams.GetOptionsAsync();
    }

    private async Task FillEditOptionsAsync(EditUserViewModel model)
    {
        model.DepartmentOptions = await _departments.GetOptionsAsync(model.DepartmentId);
        model.TeamOptions = await _teams.GetOptionsAsync(model.SupportTeamId);
    }
}
