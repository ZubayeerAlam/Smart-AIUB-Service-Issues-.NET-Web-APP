using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Account;

namespace Smart_AIUB_Service_Issue.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService accountService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    // ---------- Register ----------

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        var model = new RegisterViewModel();
        await FillRegisterOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var (result, user) = await _accountService.RegisterAsync(model);
            if (result.Succeeded && user is not null)
            {
                _logger.LogInformation("New {Role} account registered: {Email}", model.Role, user.Email);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // Something failed: show the form again with the user's input and errors.
        await FillRegisterOptionsAsync(model);
        return View(model);
    }

    // ---------- Login / Logout ----------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            // Same message as a wrong password, so attackers can't test which emails exist.
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // Step 1: check the password only (does not log in yet).
        // lockoutOnFailure: true => wrong passwords count towards the 5-attempt lockout.
        var check = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

        if (check.IsLockedOut)
        {
            _logger.LogWarning("Account locked out: {Email}", user.Email);
            ModelState.AddModelError(string.Empty, "Too many failed attempts. Your account is locked for 15 minutes.");
            return View(model);
        }

        if (!check.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // Step 2: correct password, but is the account still active?
        // (Checked after the password so strangers can't discover deactivated accounts.)
        if (!user.IsActive)
        {
            _logger.LogWarning("Deactivated user tried to log in: {Email}", user.Email);
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact the administrator.");
            return View(model);
        }

        // Step 3: create the login cookie.
        await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
        _logger.LogInformation("User logged in: {Email}", user.Email);

        // Only redirect to pages inside this site (prevents "open redirect" attacks).
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task FillRegisterOptionsAsync(RegisterViewModel model)
    {
        model.DepartmentOptions = await _accountService.GetDepartmentOptionsAsync();
        model.RoleOptions = Roles.SelfRegister.Select(r => new SelectListItem(r, r));
    }
}
