# Security checklist — AIUB Smart Campus (Release 1)

Each item says **where** it is handled and **how it is verified**.
✅ = automated test · 👀 = checked manually in the browser.

## Authentication & accounts

| # | Check | Where | Verified |
|---|---|---|---|
| A1 | Passwords stored as salted hashes (never plain text) | ASP.NET Core Identity | built-in |
| A2 | Password rules: 8+ chars, upper, lower, digit | `Authorization/IdentityConfig.cs` | ✅ `Register_rejects_weak_password…`, `ResetPassword_rejects_weak_password` |
| A3 | Lockout after 5 wrong passwords for 15 min | `IdentityConfig.cs`, `AccountController.Login` (`lockoutOnFailure: true`) | built-in; admin reset clears it ✅ `ResetPassword_sets_new_password_and_clears_lockout` |
| A4 | Self-registration can only create Student / Faculty | `AccountService.RegisterAsync` (allow-list) | ✅ `Register_rejects_roles_that_are_not_self_registrable` · 👀 tampered "Admin" option rejected |
| A5 | Deactivated users cannot log in; existing sessions end within ~3 min | `AccountController.Login`, security stamp + `SecurityStampValidatorOptions` (3 min) | ✅ `Deactivate_sets_IsActive_false_and_changes_security_stamp` · 👀 deactivated login refused |
| A6 | "Deactivated" is only revealed after a correct password | `AccountController.Login` (password checked first) | code review |
| A7 | Same error for unknown email and wrong password (no account probing) | `AccountController.Login` | code review |
| A8 | Admin cannot deactivate their own account | `UserManagementService.SetActiveAsync` | ✅ `Admin_cannot_deactivate_own_account` |
| A9 | Login cookie: HttpOnly, Secure, SameSite=Lax, 8 h sliding | `Program.cs` → `ConfigureApplicationCookie` | config |
| A10 | Open redirect blocked after login | `AccountController.Login` → `Url.IsLocalUrl` + `LocalRedirect` | code review |

## Authorization

| # | Check | Where | Verified |
|---|---|---|---|
| Z1 | Every page needs login unless marked `[AllowAnonymous]` | `Program.cs` → fallback policy | ✅ `Only_login_and_register_accept_anonymous_posts` · 👀 logged out → login page |
| Z2 | Admin pages require the Admin role | `AdminControllerBase` (`[Authorize(Policy = AdminOnly)]`) | ✅ `Admin_controller_requires_AdminOnly_policy`, `Every_non_admin_controller_is_listed_on_purpose` · 👀 student → Access denied |
| Z3 | Assets: staff & admin view, only admin edits | `AssetsController` (StaffOrAdmin + AdminOnly on Create/Edit) | ✅ `Asset_pages_require_staff_or_admin`, `Only_admins_can_add_or_edit_assets` |
| Z4 | **IDOR**: users only open issues they may see (own / own team / admin) | `IssueAuthorizationHandler` + `IssuePermissions`, used by every issue action; 404 otherwise | ✅ `IssueAuthorizationTests` (5 tests) · 👀 second student gets 404 on Details / Edit / forged Cancel |
| Z5 | Workflow actions checked again in the service (not only by hiding buttons) | `IssueWorkflowService` → `IssuePermissions.Can…` | ✅ `IssueWorkflowServiceTests` (forbidden transitions) |
| Z6 | Internal notes never reach reporters (filtered in the SQL query) | `IssueService.GetDetailsAsync` | ✅ `Internal_notes_are_hidden_from_the_reporter…` · 👀 forged internal note refused |
| Z7 | Public "Open issues" list shows no reporter names and no links | `IssueService.GetOpenIssuesAsync`, `Open.cshtml` | ✅ `OpenIssues_hides_finished_issues_and_reporter_names` · 👀 |

## Input handling

| # | Check | Where | Verified |
|---|---|---|---|
| I1 | **CSRF**: anti-forgery token checked on every POST (only exception: the read-only error page `HomeController.HttpError`) | `Program.cs` → global `AutoValidateAntiforgeryTokenAttribute` | ✅ `Only_the_read_only_error_page_skips_anti_forgery_checks` · 👀 POST without token → 400 with friendly page |
| I2 | **Over-posting**: forms bind to ViewModels with only allowed fields | `ViewModels/*` | ✅ `Form_models_have_no_fields_a_user_must_not_set` |
| I3 | Ids for create/edit come from the URL, not the form (`model.Id = 0` / `= id`) | all Create/Edit POST actions | code review |
| I4 | Values from the database win over posted values (e.g. user role) | `UserManagementService.UpdateAsync` | ✅ `Posted_role_on_the_edit_user_form_is_ignored` |
| I5 | Server-side validation of relations (room in building, staff in team, asset in same room, active lookups) | services | ✅ many service tests |
| I6 | **SQL injection**: only EF Core LINQ (parameterised), no SQL strings | whole app | code review (`grep FromSql` → none) |
| I7 | **XSS**: Razor encodes all output; no `Html.Raw` on user text; confirm texts read from `data-` attributes | views, `site.js` | code review (`grep Html.Raw` → none) |

## Platform

| # | Check | Where | Verified |
|---|---|---|---|
| P1 | No secrets in Git (connection string, admin password in User Secrets) | `secrets.json` outside the repo | `git log -S` check in M1 |
| P2 | HTTPS redirect + HSTS (outside Development) | `Program.cs` | config |
| P3 | Security headers: `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy` | `Program.cs` middleware | 👀 response headers |
| P4 | Friendly error pages, no stack traces outside Development | `UseExceptionHandler`, `UseStatusCodePagesWithReExecute`, `Error.cshtml`, `HttpError.cshtml` | 👀 404 page |
| P5 | Audit logging of admin account changes (no passwords logged) | `UsersController` (`ILogger`) | code review |
| P6 | Concurrency: two people editing one issue cannot silently overwrite each other | `Issue.RowVersion` + `DbUpdateConcurrencyException` handling | code review |
| P7 | Dependency check: no known vulnerable packages | `dotnet list package --vulnerable --include-transitive` | run in M9 |

## Known limitations (accepted for Release 1)

- No email confirmation: anyone can register as Student or **Faculty** (decision D16). Admins can deactivate fake accounts.
- No Content-Security-Policy header yet: a few pages use small inline scripts. A CSP with nonces is a good Release 2 task.
- Rate limiting is only the Identity lockout on login; no global request rate limiting.
