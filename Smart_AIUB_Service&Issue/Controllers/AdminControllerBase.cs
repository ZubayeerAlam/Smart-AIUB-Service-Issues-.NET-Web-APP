using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;

namespace Smart_AIUB_Service_Issue.Controllers;

/// <summary>
/// Base class for every admin-only controller. The [Authorize] attribute is inherited,
/// so a new admin controller is protected just by deriving from this class.
/// </summary>
[Authorize(Policy = Policies.AdminOnly)]
public abstract class AdminControllerBase : Controller
{
    /// <summary>Shows a service error next to the matching form field (or at the top of the form).</summary>
    protected void AddError(ServiceResult result)
    {
        ModelState.AddModelError(result.Field ?? string.Empty, result.Error ?? "Something went wrong.");
    }

    /// <summary>One-time message shown at the top of the next page (see _Alerts partial).</summary>
    protected void Flash(ServiceResult result, string successMessage)
    {
        if (result.Succeeded)
        {
            TempData["Success"] = successMessage;
        }
        else
        {
            TempData["Error"] = result.Error;
        }
    }

    /// <summary>Back to the list the user came from (keeps search/page), or to Index.</summary>
    protected IActionResult RedirectBack(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index");
    }
}
