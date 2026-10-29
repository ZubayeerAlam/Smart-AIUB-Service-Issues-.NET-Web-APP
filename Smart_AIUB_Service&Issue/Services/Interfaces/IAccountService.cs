using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.ViewModels.Account;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IAccountService
{
    /// <summary>
    /// Creates a Student or Faculty account. Returns the new user on success,
    /// or a failed IdentityResult with readable error messages.
    /// </summary>
    Task<(IdentityResult Result, ApplicationUser? User)> RegisterAsync(RegisterViewModel model);

    /// <summary>Active departments for the Register dropdown.</summary>
    Task<List<SelectListItem>> GetDepartmentOptionsAsync();
}
