using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services;

public class UserManagementService : IUserManagementService
{
    /// <summary>Roles an Admin can create here. Students/Faculty register themselves.</summary>
    public static readonly string[] CreatableRoles = [Roles.SupportStaff, Roles.Admin];

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Task<PagedResult<UserListItem>> GetPageAsync(string? search, string? status, string? role, int page)
    {
        var now = DateTimeOffset.UtcNow;

        // Each user has exactly one role; this sub-query reads its name.
        var query =
            from u in _db.Users.AsNoTracking().WhereStatus(status)
            let roleName = (from ur in _db.UserRoles
                            join r in _db.Roles on ur.RoleId equals r.Id
                            where ur.UserId == u.Id
                            select r.Name).FirstOrDefault()
            select new { User = u, RoleName = roleName };

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(x => x.RoleName == role);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.User.FullName.Contains(term) ||
                x.User.Email!.Contains(term) ||
                (x.User.UniversityId != null && x.User.UniversityId.Contains(term)));
        }

        return query
            .OrderBy(x => x.User.FullName)
            .Select(x => new UserListItem(
                x.User.Id,
                x.User.FullName,
                x.User.Email!,
                x.User.UniversityId,
                x.RoleName,
                x.User.Department != null ? x.User.Department.Code : null,
                x.User.SupportTeam != null ? x.User.SupportTeam.Name : null,
                x.User.IsActive,
                x.User.LockoutEnd != null && x.User.LockoutEnd > now))
            .ToPagedResultAsync(page);
    }

    public async Task<ServiceResult> CreateAsync(CreateUserViewModel model)
    {
        if (!CreatableRoles.Contains(model.Role))
        {
            return ServiceResult.Fail("Please select Support Staff or Admin.", nameof(model.Role));
        }

        int? teamId = null;
        if (model.Role == Roles.SupportStaff)
        {
            var teamIsActive = await _db.SupportTeams.AnyAsync(t => t.Id == model.SupportTeamId && t.IsActive);
            if (!teamIsActive)
            {
                return ServiceResult.Fail("Support staff must belong to an active support team.", nameof(model.SupportTeamId));
            }
            teamId = model.SupportTeamId;
        }

        var universityId = string.IsNullOrWhiteSpace(model.UniversityId) ? null : model.UniversityId.Trim();
        if (universityId is not null && await _db.Users.AnyAsync(u => u.UniversityId == universityId))
        {
            return ServiceResult.Fail("This ID is already used by another user.", nameof(model.UniversityId));
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            UniversityId = universityId,
            SupportTeamId = teamId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // User + role are saved together or not at all.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            result = await _userManager.AddToRoleAsync(user, model.Role);
        }
        if (!result.Succeeded)
        {
            return ServiceResult.FromIdentity(result);
        }

        await transaction.CommitAsync();
        return ServiceResult.Success();
    }

    public async Task<EditUserViewModel?> GetEditAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        return new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Role = await GetRoleAsync(user),
            FullName = user.FullName,
            UniversityId = user.UniversityId,
            DepartmentId = user.DepartmentId,
            SupportTeamId = user.SupportTeamId
        };
    }

    public async Task<ServiceResult> UpdateAsync(EditUserViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return ServiceResult.Fail("User not found.");
        }

        // The role always comes from the database, never from the posted form.
        var role = await GetRoleAsync(user);

        var universityId = string.IsNullOrWhiteSpace(model.UniversityId) ? null : model.UniversityId.Trim();
        if (universityId is not null && await _db.Users.AnyAsync(u => u.Id != user.Id && u.UniversityId == universityId))
        {
            return ServiceResult.Fail("This ID is already used by another user.", nameof(model.UniversityId));
        }

        if (role is Roles.Student or Roles.Faculty)
        {
            // A new department must be active; keeping the current (now inactive) one is allowed.
            if (model.DepartmentId != user.DepartmentId &&
                !await _db.Departments.AnyAsync(d => d.Id == model.DepartmentId && d.IsActive))
            {
                return ServiceResult.Fail("Please select an active department.", nameof(model.DepartmentId));
            }
            user.DepartmentId = model.DepartmentId;
            user.SupportTeamId = null;
        }
        else if (role == Roles.SupportStaff)
        {
            if (model.SupportTeamId != user.SupportTeamId &&
                !await _db.SupportTeams.AnyAsync(t => t.Id == model.SupportTeamId && t.IsActive))
            {
                return ServiceResult.Fail("Support staff must belong to an active support team.", nameof(model.SupportTeamId));
            }
            user.SupportTeamId = model.SupportTeamId;
            user.DepartmentId = null;
        }
        else
        {
            // Admins belong to neither.
            user.DepartmentId = null;
            user.SupportTeamId = null;
        }

        user.FullName = model.FullName.Trim();
        user.UniversityId = universityId;
        return ServiceResult.FromIdentity(await _userManager.UpdateAsync(user));
    }

    public async Task<ServiceResult> SetActiveAsync(string id, bool isActive, string currentUserId)
    {
        if (!isActive && id == currentUserId)
        {
            return ServiceResult.Fail("You cannot deactivate your own account.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return ServiceResult.Fail("User not found.");
        }

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded && !isActive)
        {
            // Invalidates the user's current login cookie (checked every 3 minutes).
            result = await _userManager.UpdateSecurityStampAsync(user);
        }

        return ServiceResult.FromIdentity(result);
    }

    public async Task<ResetPasswordViewModel?> GetResetPasswordAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user is null
            ? null
            : new ResetPasswordViewModel { Id = user.Id, DisplayName = $"{user.FullName} ({user.Email})" };
    }

    public async Task<ServiceResult> ResetPasswordAsync(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return ServiceResult.Fail("User not found.");
        }

        // Identity's normal reset flow: create a one-time token, then use it immediately.
        // This also changes the security stamp, so old logins of this user end.
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return ServiceResult.FromIdentity(result);
        }

        // A reset also lifts a lockout caused by too many wrong passwords.
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        return ServiceResult.Success();
    }

    private async Task<string> GetRoleAsync(ApplicationUser user) =>
        (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "";
}
