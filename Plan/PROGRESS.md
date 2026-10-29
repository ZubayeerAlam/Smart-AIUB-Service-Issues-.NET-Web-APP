# AIUB Smart Campus — Release 1 Progress Tracker

Full plan: [Release1-Plan.md](Release1-Plan.md)

**Legend:** ✅ done · 🔄 in progress · ⬜ not started

| Milestone | Status |
|---|---|
| Planning | ✅ |
| M0 — Repo hygiene & setup | ✅ |
| M1 — Database design & first migration | ✅ (seeding moved to M2) |
| M2 — Auth, roles & seed data | ✅ (committed) |
| M3 — Master data & user management | ✅ (committed `2418627`; your Admin browser check still open) |
| M4 — Issue reporting | ✅ (committed `ab06373`) |
| M5 — Review, workflow, history, comments | ✅ (not committed yet; your Admin/Staff browser check open) |
| M6 — Assets & maintenance | ⬜ **← NEXT** |
| M7 — Dashboards | ⬜ |
| M8 — Search, filters & reports | ⬜ |
| M9 — Hardening & docs | ⬜ |

---

## ✅ Planning
- [x] PRD, requirements, permission matrix, issue lifecycle, entity list
- [x] Clarifying questions answered (self-register, team assignment, **no attachments**, admin-managed rooms, etc.)
- [x] Plan approved
- [x] Decisions D1–D17 accepted with the recommended defaults (see plan, section 13)

## ✅ M0 — Repo hygiene & setup
- [x] NuGet packages added, all **10.0.14**: EF Core SqlServer, EF Core Tools, Identity.EntityFrameworkCore, Diagnostics.EntityFrameworkCore
- [x] Security pin: `System.Security.Cryptography.Pkcs` 10.0.14 (fixes CVE-2026-84008 / CVE-2026-70162 from SqlClient)
- [x] Test project `Smart_AIUB_Service_Issue.Tests` (xUnit + EF Core SQLite) added to the solution
- [x] `.gitignore` added (bin/, obj/, .vs/ ignored)
- [x] Connection string fixed (`Server=` was missing) and moved to **User Secrets**
- [x] Local tool `dotnet-ef` 10.0.14 (`dotnet-tools.json`)

## ✅ M1 — Database design & first migration
- [x] Enums: IssueStatus, IssuePriority, AssetStatus, MaintenanceType (stored as text)
- [x] 12 entities in `Models/Entities/`
- [x] Fluent configurations in `Data/Configurations/` (lengths, unique indexes, Restrict deletes, RowVersion)
- [x] `ApplicationDbContext` (IdentityDbContext) registered in `Program.cs`
- [x] `InitialCreate` migration applied — 19 tables in `AIUB_Service&IssuesDb` (SQL Server 2025 Express)
- [x] Build: 0 warnings, 0 errors · Tests: 1/1 passed
- [x] Committed: `5f71f41 Add EF Core data model and initial database migration`
- [ ] Push to GitHub

## ✅ M2 — Auth, roles & seed data
**Decisions:** 4 roles kept (Student, Faculty, SupportStaff, Admin). Deactivated = `IsActive = false`. A logged-in deactivated user is signed out within ~3 minutes (security stamp check).

- [x] Identity registered in `Program.cs`. Rules in `Authorization/IdentityConfig.cs`: password 8+ chars with upper, lower and digit; lockout after 5 tries for 15 min; unique email
- [x] Cookie settings: HttpOnly, Secure, SameSite=Lax, 8 h sliding; security stamp re-checked every 3 min
- [x] Secure by default: every page needs login unless `[AllowAnonymous]`; global anti-forgery (CSRF) check on POSTs
- [x] `Authorization/Roles.cs` constants + policies `AdminOnly`, `StaffOrAdmin`
- [x] `Data/Seed/DbSeeder.cs`: 4 roles, admin (email/password from User Secrets `SeedAdmin:*`), 9 departments, 11 buildings (D, C, Annex 1–9), 99 sample rooms, 4 support teams, 6 categories (with default team), 6 asset types. Only adds what is missing.
- [x] `AccountService.RegisterAsync`: only Student/Faculty allowed, active department, unique Student ID, user + role saved in one transaction
- [x] `AccountController`: Register, Login, Logout, AccessDenied, safe return URL
- [x] Deactivated users blocked at login (checked after the password)
- [x] Navbar: app name, email + role badge, Log out / Register / Log in
- [x] Tests: 9/9 passed (Admin/SupportStaff/blank role rejected, Student/Faculty created, inactive department, duplicate ID, weak password)
- [x] Browser check: tampered "Admin" role rejected · Student registered and logged in · deactivated student blocked
- [ ] **You:** log in as Admin (`admin@smartcampus.test`; password in Visual Studio → right-click project → Manage User Secrets)
- [x] Commit M2 (`ea1b406`)

## 🔄 M3 — Master data & user management
- [x] Shared helpers in `Common/`: `PagedResult<T>` + `ToPagedResultAsync`, `ServiceResult`, `WhereStatus` / `SetActiveAsync` (via `IActivatable`)
- [x] Add / edit / deactivate / search / page: **Departments, Buildings, Rooms, Issue categories, Support teams** (one service + controller + `Index` and `Form` views each)
- [x] Rules: unique codes/names, room number unique per building, no new rooms in inactive buildings, default team must be active, inactive items kept on old records but hidden from dropdowns
- [x] User management: list (search, role, status, "Locked" badge), create Support Staff / Admin, edit name / ID / department / team, deactivate (cannot deactivate yourself; signs user out within ~3 min), reset password (also unlocks)
- [x] `AdminControllerBase` with `[Authorize(Policy = AdminOnly)]`: every admin controller inherits it
- [x] Shared partials: `_Pagination`, `_ListFilter`, `_ActiveBadge`, `_ActiveToggle` (POST + confirm), `_Alerts`
- [x] Navbar **Manage** menu (Admin only)
- [x] Tests: **42/42 passed** (33 new: duplicates, paging, status filter, rooms per building, staff needs team, self-deactivate blocked, security stamp changes, reset unlocks, every admin controller requires AdminOnly)
- [x] Browser check: logged out → login page · Student → "Access denied" on `/Departments`
- [ ] **You:** log in as Admin and click through Manage → each page
- [x] Commit M3 (`2418627`)

## ✅ M4 — Issue reporting
- [x] Report form: Category, Building → Floor & room (cascading dropdown via `GET /Issues/Rooms?buildingId=`), Title, Description
- [x] Server checks: active category, active room in an active building, **room belongs to the chosen building**
- [x] First timeline entry ("Submitted · Issue reported") written with the issue
- [x] My issues (search incl. ticket no. `ISS-000123`, status filter, paging) and public **Open issues** (summary only, no reporter, no link)
- [x] Issue detail page: description, timeline, location, team/technician, Edit + Cancel buttons when allowed
- [x] Edit / cancel only while Submitted (reporter or admin); cancel writes a history row
- [x] `Authorization/IssuePermissions.cs` (all rules in one place) + `IssueAuthorizationHandler` (resource-based) + `IssueActorService`
- [x] `IssueWorkflowService` started (Cancel); M5 adds the other transitions
- [x] Times shown in Asia/Dhaka (`Common/DhakaTime.cs`); ticket numbers from Id (`Common/IssueDisplay.cs`)
- [x] Navbar: Report issue · My issues · Open issues; collapses to ☰ below 992px; filter buttons wrap on phones
- [x] Tests: **60/60 passed** (18 new: room/building mismatch, inactive room/category, edit refused after review, other user can't edit, cancel rules, IDOR via `IAuthorizationService`, staff only see their team's issues)
- [x] Browser check: report with cascading rooms · edit pre-filled · second student gets **404** on Details / Edit / forged Cancel POST · issue unchanged in DB · phone layout
- [x] Commit M4 (`ab06373`)

## ✅ M5 — Review, workflow, history, comments
- [x] `IssueWorkflowService`: assign / reassign, reject, take, start, hold, resume, resolve, cancel, close, reopen. Each checks `IssuePermissions`, validates input and writes one history row in the same save; concurrency conflicts (RowVersion) give a friendly "reload" message
- [x] Rules: priority + active team required; technician must be active staff of that team; reject/hold/reopen need a reason; resolve needs a note (10+ chars); reassign possible while Assigned / In progress / On hold
- [x] Reporter: close (confirm fix) or reopen within **7 days** (`Issues:ReopenWindowDays` in appsettings) → back to the same team and technician, `ReopenCount` +1
- [x] Admin: **Review queue** (oldest first) and **All issues** (status, priority, team, search) under the navbar **Issues** menu; Review/Reassign panel on the issue page with team-filtered technician list
- [x] Staff: **Team queue** with tabs Assigned to me / Unassigned / Whole team; Work panel (Take, Start, Hold, Resume, Resolve)
- [x] Comments with **internal notes** (staff/admin only; filtered out in the database query for reporters; yellow "Internal note" style); comments close when the issue is finished
- [x] Issue page shows only the actions the current user may do right now; priority badges; "Reopened n×" badge
- [x] Tests: **88/88 passed** (28 new: full lifecycle history, every forbidden transition, reopen window, internal notes hidden, team queue scopes)
- [x] Browser check (reporter): comment posted · no admin/staff panels · forged internal note refused
- [ ] **You:** as Admin: Issues → Review queue → open ISS-000001 → assign. Then create a Support Staff user in that team, log in as them and use Team queue → Take → Start → Resolve
- [ ] Commit M5

## ⬜ M6 — Assets & maintenance ← NEXT
- [ ] Asset types CRUD, asset CRUD (Admin)
- [ ] Link issue → asset (same room), maintenance records, asset history page
- [ ] Tests: unique tag, same-room rule

## ⬜ M7 — Dashboards
- [ ] Reporter, Support Staff and Admin dashboards

## ⬜ M8 — Search, filters & reports
- [ ] Filters on all lists
- [ ] Date-range reports: issues, resolution time, maintenance cost
- [ ] Tests: date boundaries (UTC ↔ Asia/Dhaka)

## ⬜ M9 — Hardening & docs
- [ ] Error pages, logging review, security checklist (IDOR, CSRF, over-posting)
- [ ] README: setup, User Secrets, migrations, screenshots
- [ ] Full test run

---

## Change log
| Date | What changed |
|---|---|
| 2026-10-29 | Planning done; M0 and M1 completed; Plan folder created |
| 2026-10-29 | M2 implemented: Identity, roles, seeder, register/login/logout, tests (9/9) |
| 2026-10-29 | UI redesign: navy/blue theme, bold brand, full-screen hero, split-screen login/register, phone layout |
| 2026-10-29 | M3 implemented: master data CRUD, user management, pagination, tests (42/42) |
| 2026-10-29 | M4 implemented: issue reporting, lists, details, edit/cancel, resource-based authorization, tests (60/60) |
| 2026-10-29 | M5 implemented: full workflow, review queue, team queue, comments with internal notes, tests (88/88) |
