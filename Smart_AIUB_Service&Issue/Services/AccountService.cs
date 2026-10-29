using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Account;

namespace Smart_AIUB_Service_Issue.Services;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<(IdentityResult Result, ApplicationUser? User)> RegisterAsync(RegisterViewModel model)
    {
        // Never trust the posted role: the dropdown only shows Student/Faculty,
        // but anyone can edit the HTML or send a hand-made request with "Admin".
        if (!Roles.SelfRegister.Contains(model.Role))
        {
            return (Fail("InvalidRole", "Please select Student or Faculty."), null);
        }

        var departmentIsValid = await _db.Departments
            .AnyAsync(d => d.Id == model.DepartmentId && d.IsActive);
        if (!departmentIsValid)
        {
            return (Fail("InvalidDepartment", "Please select a valid department."), null);
        }

        var universityId = model.UniversityId.Trim();
        if (await _db.Users.AnyAsync(u => u.UniversityId == universityId))
        {
            return (Fail("DuplicateUniversityId", "This Student / Employee ID is already registered."), null);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            UniversityId = universityId,
            DepartmentId = model.DepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Creating the user and giving it a role are two saves. The transaction
        // makes sure we never end up with a user that has no role.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return (result, null);
        }

        result = await _userManager.AddToRoleAsync(user, model.Role);
        if (!result.Succeeded)
        {
            return (result, null); // transaction is rolled back when disposed
        }

        await transaction.CommitAsync();
        return (IdentityResult.Success, user);
    }

    public async Task<List<SelectListItem>> GetDepartmentOptionsAsync()
    {
        return await _db.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem($"{d.Name} ({d.Code})", d.Id.ToString()))
            .ToListAsync();
    }

    private static IdentityResult Fail(string code, string description) =>
        IdentityResult.Failed(new IdentityError { Code = code, Description = description });
}
