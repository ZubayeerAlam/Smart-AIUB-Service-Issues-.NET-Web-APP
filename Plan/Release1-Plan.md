# AIUB Smart Campus — Service & Maintenance Management System
## Release 1 Planning Document (no code yet)

---

## 0. Context

You want a portfolio-grade ASP.NET Core MVC (.NET 10) app where Students/Faculty report campus problems, Admins review and assign them to support teams, and Support Staff work and resolve them. It also includes an asset registry and maintenance log. This document is the plan only. Nothing is coded, installed or migrated until you approve it.

### What I found in the project directory (inspected, nothing changed)

| Item | Current state | Note |
|---|---|---|
| Solution | `Smart_AIUB_Service&Issue.slnx` (one project) | New `.slnx` format, fine for VS 2026 / .NET 10 |
| Project | `Smart_AIUB_Service&Issue/…csproj`, `net10.0`, Nullable + ImplicitUsings on | Default MVC template |
| Root namespace | `Smart_AIUB_Service_Issue` | `&` already replaced by `_` in the namespace, which is good |
| NuGet packages | **None** | No EF Core or Identity yet |
| `Program.cs` | Template default: MVC, HTTPS redirection, `UseAuthorization` with no authentication | Needs Identity, DbContext and services added |
| `appsettings.json` | No connection string | Good: the connection string goes in User Secrets, not here |
| `.gitignore` | **Missing** | Risk: `.vs/`, `bin/`, `obj/` and `*.csproj.user` would be committed. Fix in Milestone 0 |
| Git | 1 commit (README only); project files untracked | |
| .NET SDKs | 10.0.301, 10.0.400 | OK for .NET 10 / EF Core 10 |

You chose to **keep the name with `&`**. That works in Visual Studio. In CLI commands (`dotnet ef …`) we will always put the project path in quotes. The test project will be named `Smart_AIUB_Service_Issue.Tests` (no `&`).

---

## 1. Product Requirements Document (PRD)

**Problem.** Campus problems (dead projectors, lab PCs, Wi-Fi, electrical, furniture) get reported informally. They get lost, nobody knows who owns them, and nobody can see the repair history of equipment.

**Goal.** Give the campus one place to report a problem in under a minute, route it to the right support team, track it to closure with a full audit trail, and keep a maintenance history per asset.

**Users.** Students, Faculty (reporters), Support Staff (fixers), Admins (reviewers and system managers).

**Success criteria (Release 1)**
- A reporter can submit an issue with category, building, room and description, then track its status.
- Every issue has an owner team after review, and every status change is recorded with who, when and why.
- Admin can answer questions such as "How many issues per building or category last month?" and "What is the repair history of projector X?"
- All access rules are enforced on the server, not only hidden in the UI.

**Out of scope for Release 1:** file attachments (removed by your decision), email/SMS notifications, SLA timers and auto-escalation, auto-close jobs, Supervisor role, CSV/PDF export, mobile app, external APIs, integration with AIUB systems, and the Release 2/3 features.

---

## 2. Requirements

### 2.1 Confirmed functional requirements (from your spec and answers)

| # | Requirement |
|---|---|
| F1 | Login, logout and role-based authorization with ASP.NET Core Identity. Roles: Student, Faculty, SupportStaff, Admin. |
| F2 | Students and Faculty **self-register** with any email (no domain restriction) and choose Student or Faculty plus a Department. No approval step. |
| F3 | Admin creates SupportStaff and Admin accounts and assigns Support Staff to a Support Team. |
| F4 | Admin manages Departments (academic: CSE, EEE, BBA…). Departments belong to user profiles only. |
| F5 | Admin manages Buildings (e.g. D-Building, C-Building, Annex 1–9) and Rooms (building + floor + room number). |
| F6 | Admin manages Issue Categories (Projector, Computer Lab, Wi-Fi/Network, Electrical, Furniture, Other). |
| F7 | **Issue form:** Category, Building, Room (dropdown filtered by building, shows floor), Description. **No attachments.** |
| F8 | Reporters see their own issues in full and a **public read-only list** of open issues (no reporter names, no comments). |
| F9 | Reporter can **edit or cancel** an issue only while its status is `Submitted`. |
| F10 | Admin reviews a submitted issue: sets **Priority** and assigns a **Support Team** (and optionally one staff member), or rejects it with a reason. |
| F11 | Support Staff can take an unassigned issue in their team's queue, move it through the work statuses, and must enter a resolution note when resolving. |
| F12 | After `Resolved`, the reporter can **Close** it (confirm the fix) or **Reopen** it within N days, with a reason. Admin can also close it. |
| F13 | Status history is recorded automatically for every transition. |
| F14 | Comments on issues. Admin/Support can mark a comment **internal** (hidden from the reporter). |
| F15 | Asset registry: assets have a tag, type and room, and a status. |
| F16 | Maintenance records are linked to an asset and optionally to an issue. **Staff/Admin link an issue to an asset**; reporters do not. |
| F17 | Role-specific dashboards. |
| F18 | Search, filter, pagination on lists. Date-range reports. |
| F19 | Server-side validation, error handling, basic automated tests (xUnit + SQLite in-memory). |

### 2.2 Non-functional requirements

| Area | Requirement |
|---|---|
| Security | Server-side authorization on every action. Anti-forgery tokens on all POSTs. ViewModels only (no binding to entities). Identity password and lockout policy. HTTPS + HSTS. No secrets in Git. |
| Data integrity | FK constraints, unique indexes, required fields, string max lengths. `Restrict` delete on business data. Optimistic concurrency on `Issue`. |
| Performance | All lists paginated (default 10–20 rows). Indexes on filter columns. `AsNoTracking` for read-only queries. Async EF calls. |
| Usability | Bootstrap 5 responsive layout. Colour-coded status/priority badges. Clear validation messages. |
| Maintainability | Thin controllers, service layer, Fluent API configurations per entity, consistent naming. |
| Auditability | Immutable status history with user and timestamp. `CreatedAt`/`UpdatedAt` on main tables. |
| Time | Store **UTC** in the database and display in **Asia/Dhaka** time. |
| Reliability | Global exception handler page. Structured logging with the built-in `ILogger`. Friendly 403/404 pages. |

### 2.3 Recommendations (my suggestions, need your OK)
- **R1.** Add a short **Title** field (max 100 chars) to the issue form so list views are readable. Alternative: auto-title "Category – Room", for example "Projector – D-301".
- **R2.** Each Category gets an optional **Default Support Team** that pre-selects the team on the review screen to speed up admin work.
- **R3.** Display a **ticket number** derived from the Id (`ISS-000123`) instead of storing a separate column.
- **R4.** Store enums (Status, Priority, AssetStatus…) **as strings** in SQL Server so the data is readable in SSMS.
- **R5.** **Deactivate instead of delete** for master data (Buildings, Rooms, Categories, Teams, Departments, Users) so old issues keep valid references.
- **R6.** Build a **custom `AccountController` + Razor views** for Register/Login instead of the scaffolded Identity UI. Registration needs Department and Role fields, and it is easier to learn from.
- **R7.** Reopen window = **7 days** (configurable in `appsettings.json`).

### 2.4 Assumptions (please correct any that are wrong)
- **A1.** Students and Faculty have identical permissions in Release 1. The role is stored for reports and future use.
- **A2.** A user has exactly **one role**.
- **A3.** A Support Staff member belongs to **one** Support Team.
- **A4.** No email sending of any kind (no confirmation, no password reset email). Admin can reset a user's password.
- **A5.** One campus only. Building → Room is enough (floor is a field on Room).
- **A6.** Asset types are an admin-managed list (Projector, Desktop PC, Router, AC, Chair…).
- **A7.** "Basic automated tests" means unit/service tests focused on business rules, not UI tests.

---

## 3. Roles & Permission Matrix

Legend: ✅ allowed · 🔸 own/limited · ❌ denied

| Capability | Student / Faculty | Support Staff | Admin |
|---|---|---|---|
| Self-register | ✅ | ❌ (admin-created) | ❌ (admin-created/seeded) |
| Submit issue | ✅ | ✅ (optional, see D9) | ✅ |
| View own issues (full detail) | ✅ | ✅ | ✅ |
| View public open-issue list | ✅ | ✅ | ✅ |
| View any issue in full | ❌ | 🔸 issues of their team | ✅ |
| Edit / cancel issue | 🔸 own, while Submitted | ❌ | ✅ |
| Review: set priority, assign team/staff, reject | ❌ | ❌ | ✅ |
| Take an issue from the team queue | ❌ | 🔸 own team | ✅ (assign) |
| Start / hold / resolve | ❌ | 🔸 assigned to them or their team | ✅ |
| Close or reopen a resolved issue | 🔸 own issue | ❌ | ✅ (close) |
| Public comments | 🔸 own issues | 🔸 team issues | ✅ |
| Internal comments (see & write) | ❌ | 🔸 team issues | ✅ |
| Link issue → asset | ❌ | 🔸 team issues | ✅ |
| View assets | ❌ | ✅ | ✅ |
| Create / edit assets | ❌ | ❌ (see D10) | ✅ |
| Add maintenance record | ❌ | ✅ | ✅ |
| Manage buildings, rooms, categories, departments, teams | ❌ | ❌ | ✅ |
| Manage users (create staff, deactivate, reset password) | ❌ | ❌ | ✅ |
| Dashboard | Reporter dashboard | Staff dashboard | Admin dashboard |
| Date-range reports | ❌ | ❌ | ✅ |

**How we enforce it:** `[Authorize(Roles/Policy)]` on controllers and actions for role checks. A **resource-based check** (`IssueAuthorizationHandler`) for "is this my issue / my team's issue". This stops users from changing the id in the URL to open someone else's issue (an IDOR attack).

---

## 4. Release 1 Feature Breakdown

1. **Authentication & Authorization:** register (Student/Faculty + Department), login, logout, access-denied page, role seeding, admin seeding (credentials from User Secrets), policies, lockout after 5 failed attempts.
2. **User & Department management:** department CRUD and deactivate. User list with search/filter by role/department/active. Create Support Staff/Admin. Assign team. Activate/deactivate. Reset password.
3. **Building & Room management:** building CRUD (code + name). Room CRUD (building, floor, room number, optional room name/type). Unique room number per building.
4. **Issue reporting & categories:** category CRUD. Report form with Building → Room cascading dropdown (small JS `fetch` to a JSON endpoint). My Issues list. Public open-issue list. Edit/cancel while Submitted. Priorities are set during review. *(Attachments removed.)*
5. **Review & assignment:** admin "Review queue" (Submitted issues). Set priority. Assign team (pre-filled from category default) and optional staff. Reject with reason. Reassign team/staff later.
6. **Controlled status transitions:** one `IssueWorkflowService` holds all rules (section 5). Controllers never set `Status` directly.
7. **Status history & comments:** automatic history row per transition. Comment thread on the detail page. Internal flag for staff.
8. **Asset registry:** asset types CRUD. Assets CRUD with unique tag, room, status, serial no., purchase date, warranty date. Search/filter by building/room/type/status.
9. **Maintenance records:** add a record to an asset (type, description, date, performed by, cost, optional linked issue). Asset detail page shows its history and linked issues.
10. **Dashboards:**
    - *Reporter:* my counts by status, recent issues.
    - *Staff:* assigned to me, team queue (unassigned), in progress, on hold.
    - *Admin:* awaiting review, open by priority, counts by category/building, recently resolved.
11. **Search, filter, pagination, reports:** reusable `PagedResult<T>` + `_Pagination` partial. Filters on status/priority/category/building/team/date. Reports for a date range:
    - (a) issues by status/category/building/team
    - (b) average time to resolve
    - (c) maintenance records and total cost by asset/type
12. **Validation, errors, tests:** DataAnnotations on ViewModels plus business-rule validation in services. Global exception handler, status-code pages, xUnit tests.

---

## 5. Issue Lifecycle & Allowed Transitions

**Statuses:** `Submitted` → `Assigned` → `InProgress` ⇄ `OnHold` → `Resolved` → `Closed`. The other end states are `Rejected` and `Cancelled`.

```
            ┌──────────► Cancelled (reporter, only from Submitted)
            │
Submitted ──┼──────────► Rejected (admin, reason required)
            │
            └─► Assigned ─► InProgress ⇄ OnHold
                   ▲            │
                   │            ▼
                   └─(reopen)─ Resolved ─► Closed
```

| From | To | Who | Required / side effects |
|---|---|---|---|
| Submitted | Assigned | Admin | Priority + Team required, staff optional |
| Submitted | Rejected | Admin | Reason required (e.g. duplicate, not a campus issue) |
| Submitted | Cancelled | Reporter (own) / Admin | Optional reason |
| Assigned | Assigned (reassign) | Admin | Change of team/staff, logged in history |
| Assigned (team only) | Assigned (staff = me) | Support Staff of that team | "Take" the issue, logged |
| Assigned | InProgress | Assigned staff / team staff / Admin | Sets staff to the actor if empty |
| InProgress | OnHold | Assigned staff / Admin | Reason required (e.g. waiting for parts) |
| OnHold | InProgress | Assigned staff / Admin | — |
| InProgress | Resolved | Assigned staff / Admin | **Resolution note required**, sets `ResolvedAt` |
| Resolved | Closed | Reporter (own) / Admin | Sets `ClosedAt` |
| Resolved | Assigned (reopen) | Reporter (own), within 7 days | Reason required, `ReopenCount++`, same team/staff kept |

`Closed`, `Rejected` and `Cancelled` are **final**. Any transition not in this table is refused by the service with a clear error message. Every successful transition writes one `IssueStatusHistory` row in the same database transaction.

---

## 6. Preliminary Entity List

*(For review only. Columns, lengths and indexes get finalised in the database-design milestone, before any migration.)*

| Entity | Purpose | Key fields (draft) |
|---|---|---|
| `ApplicationUser` (extends `IdentityUser`) | Login + profile | FullName, UniversityId (student/employee ID, unique), DepartmentId?, SupportTeamId?, IsActive, CreatedAt |
| Identity tables (`AspNetRoles`, `AspNetUserRoles`, …) | Roles and user-role link | Provided by Identity |
| `Department` | Academic department for user profiles | Code (unique), Name, IsActive |
| `Building` | Campus building | Code (unique, e.g. "D", "ANX-1"), Name, IsActive |
| `Room` | Specific location | BuildingId, Floor (int), RoomNumber, Name?, IsActive. Unique (BuildingId, RoomNumber) |
| `IssueCategory` | Type of problem | Name (unique), Description?, DefaultSupportTeamId?, IsActive |
| `SupportTeam` | Group of support staff | Name (unique), Description?, IsActive |
| `Issue` | The reported problem (core table) | Title*, Description, CategoryId, RoomId, ReporterId, Status, Priority?, AssignedTeamId?, AssignedStaffId?, AssetId?, ResolutionNote?, CreatedAt, UpdatedAt, ResolvedAt?, ClosedAt?, ReopenCount, RowVersion |
| `IssueStatusHistory` | Audit trail of transitions | IssueId, FromStatus?, ToStatus, ChangedById, ChangedAt, Note? |
| `IssueComment` | Discussion on an issue | IssueId, AuthorId, Body, IsInternal, CreatedAt |
| `AssetType` | Kind of asset | Name (unique), IsActive |
| `Asset` | Physical item in a room | AssetTag (unique), Name, AssetTypeId, RoomId, SerialNumber?, Status (Active / UnderMaintenance / OutOfService / Retired), PurchaseDate?, WarrantyExpiry?, Notes? |
| `MaintenanceRecord` | Work done on an asset | AssetId, IssueId?, PerformedById, Type (Corrective / Preventive / Inspection), Description, PerformedAt, Cost? (decimal 10,2), CreatedAt |

**Enums (stored as strings):** `IssueStatus`, `IssuePriority` (Low/Medium/High/Critical), `AssetStatus`, `MaintenanceType`.

**Relationship rules:** All FKs use `DeleteBehavior.Restrict`, except comments and history, which cascade from `Issue` (in practice issues are never deleted). `RowVersion` is a SQL Server `rowversion` column. It prevents two admins from overwriting each other's assignment at the same time.

**Planned indexes:** `Issue(Status)`, `Issue(ReporterId)`, `Issue(AssignedTeamId, Status)`, `Issue(AssignedStaffId, Status)`, `Issue(CreatedAt)`, `Issue(RoomId)`, `Issue(CategoryId)`, `IssueStatusHistory(IssueId, ChangedAt)`, `Asset(RoomId)`, `MaintenanceRecord(AssetId, PerformedAt)`, plus the unique indexes listed above.

---

## 7. Business Rules & Security Requirements

### Business rules
1. Only active Categories, Buildings and Rooms can be chosen on new issues or assets.
2. The selected Room must belong to the selected Building (checked on the server, not only by the dropdown).
3. Description is required, 10–2000 characters. Title (if approved) is required, max 100 characters.
4. Priority and Team are required when assigning. An assigned staff member must belong to the assigned team and be active.
5. Status changes happen only through `IssueWorkflowService` and follow the table in section 5.
6. Reopen is allowed only by the reporter, only from Resolved, and only within the configured number of days.
7. Internal comments are never sent to the reporter's view. They are filtered in the query, not only hidden in Razor.
8. A linked asset must be in the same room as the issue (recommended rule; see D11).
9. Deactivated users cannot log in. Their past issues and records stay.
10. Self-registration can create only Student or Faculty. The role value is validated against an allow-list on the server.

### Security requirements
- Connection string and seed admin password live in **User Secrets** (development). They are never in `appsettings.json` or Git.
- Add the standard .NET `.gitignore` before the first code commit.
- Global `AutoValidateAntiforgeryToken` filter (CSRF protection on every POST).
- Identity password policy (minimum 8 characters, digit, upper/lower case) and lockout (5 attempts / 15 minutes).
- Resource-based authorization for issue detail, edit and actions (prevents IDOR).
- ViewModels with only the bindable fields (prevents over-posting, e.g. a reporter posting `Status=Closed`).
- Razor auto-encoding for output (XSS). No `Html.Raw` on user content.
- EF Core parameterised queries only. No string-built SQL.
- Production error page shows no stack traces. HSTS is on outside Development.
- Cookie auth: HttpOnly, Secure, SameSite=Lax, sliding expiration.

---

## 8. Proposed MVC Folder Structure

```
Smart_AIUB_Service&Issue/                     (solution folder)
├─ .gitignore                                 (NEW)
├─ Smart_AIUB_Service&Issue.slnx
├─ Smart_AIUB_Service&Issue/                  (web project – existing)
│  ├─ Authorization/      Roles.cs, Policies.cs, IssueAuthorizationHandler.cs
│  ├─ Common/             PagedResult.cs, ServiceResult.cs, DateTimeExtensions.cs
│  ├─ Controllers/        Account, Home, Dashboard, Issues, IssueReview, Comments,
│  │                      Users, Departments, Buildings, Rooms, Categories,
│  │                      SupportTeams, AssetTypes, Assets, Maintenance, Reports
│  ├─ Data/
│  │  ├─ ApplicationDbContext.cs
│  │  ├─ Configurations/  one IEntityTypeConfiguration<T> per entity
│  │  ├─ Migrations/      (generated later)
│  │  └─ Seed/            DbSeeder.cs (roles, admin, sample buildings/categories)
│  ├─ Models/
│  │  ├─ Entities/        Issue.cs, Room.cs, Asset.cs, …
│  │  └─ Enums/           IssueStatus.cs, IssuePriority.cs, …
│  ├─ Services/
│  │  ├─ Interfaces/      IIssueService, IIssueWorkflowService, IAssetService, …
│  │  └─ (implementations) IssueService, IssueWorkflowService, LookupService,
│  │                      AssetService, MaintenanceService, DashboardService,
│  │                      ReportService, UserManagementService
│  ├─ ViewModels/         Account/, Issues/, Assets/, Admin/, Dashboard/, Reports/
│  ├─ Views/              one folder per controller + Shared/_Pagination,
│  │                      _StatusBadge, _PriorityBadge, AccessDenied
│  ├─ wwwroot/            (existing: Bootstrap 5, jQuery validation)
│  ├─ appsettings.json    (no secrets)
│  └─ Program.cs
└─ Smart_AIUB_Service_Issue.Tests/            (NEW xUnit project)
   ├─ TestDbFactory.cs    (SQLite in-memory context)
   └─ Services/           IssueWorkflowServiceTests.cs, IssueServiceTests.cs, …
```

**Why no Areas?** Areas (a separate `Admin` area) add routing complexity. Role policies on controllers give the same protection, and the code is simpler to follow.

**Why no generic repository?** `DbContext` already *is* a repository + unit of work. Services use it directly, which matches your "no unnecessary patterns" rule.

---

## 9. NuGet Packages

All EF Core and Identity packages use the **same latest stable 10.0.x patch** at install time.

| Package | Project | Purpose |
|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | Web | EF Core provider for SQL Server 2022 Express |
| `Microsoft.EntityFrameworkCore.Tools` | Web | `Add-Migration` / `Update-Database` in the VS Package Manager Console (includes `.Design`) |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Web | Identity stores (users/roles) backed by EF Core |
| `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` | Web (optional) | Friendly DB/migration error page in Development |
| `xunit` / `xunit.runner.visualstudio` / `Microsoft.NET.Test.Sdk` | Tests | Test framework + VS Test Explorer support (template defaults) |
| `Microsoft.EntityFrameworkCore.Sqlite` | Tests | In-memory relational DB for service tests |

**Not needed:** `Microsoft.AspNetCore.Identity.UI` (we build our own account views), AutoMapper (manual mapping is clearer for learning), MediatR, any MySQL provider, any file-upload library.

**Trade-off for SQLite tests:** SQLite does not support SQL Server `rowversion` or some SQL Server column types. The test context will override those few settings. Concurrency behaviour is checked manually against SQL Server.

---

## 10. Milestone Plan (implementation + testing)

Each milestone ends with a **build**, any **tests** written so far, a **manual check in the browser**, and **a Git commit**. You review each one before we start the next. I will only report "builds" or "tests pass" after actually running them.

| # | Milestone | Main work | Tests / verification |
|---|---|---|---|
| **M0** | Repo hygiene & setup | Add `.gitignore`. Init User Secrets. Install packages from section 9. Add the connection string to secrets (`Server=.\SQLEXPRESS;Database=AiubSmartCampus;Trusted_Connection=True;TrustServerCertificate=True`). | `dotnet build`. Confirm `.vs/`, `bin/`, `obj/` are ignored and secrets are not in Git. |
| **M1a** | **Database design review** | Detailed ERD + column/length/index spec (document only). | **Your approval required** |
| **M1b** | Entities & first migration | Entities, enums, Fluent configurations, `ApplicationDbContext`, `InitialCreate` migration, seeder (roles, admin, sample buildings/rooms/categories/teams). | Migration applies to SQL Express. Inspect tables in SSMS. Seeder runs safely more than once. |
| **M2** | Auth & roles | Custom Register/Login/Logout, AccessDenied, cookie and lockout settings, policies, role-aware navbar. | Manual: register Student, log in as Admin. Tests: registration rejects role "Admin". |
| **M3** | Master data & users | CRUD for Departments, Buildings, Rooms, Categories, Support Teams. User management (create staff, assign team, deactivate, reset password). Reusable pagination. | Unique-constraint and deactivation tests. Non-admin gets 403. |
| **M4** | Issue reporting | Report form with cascading Building→Room, My Issues, public open list, detail page, edit/cancel while Submitted. | Tests: room/building mismatch rejected, edit refused after review, user cannot open another user's issue. |
| **M5** | Review, workflow, history, comments | `IssueWorkflowService`, review queue, assign/reject/reassign, take/start/hold/resolve/close/reopen, history timeline, comments + internal flag. | **Main test suite:** every allowed and forbidden transition, reopen window, internal-comment filtering, history row written. |
| **M6** | Assets & maintenance | Asset types, asset CRUD, link issue→asset, maintenance records, asset history page. | Tests: unique tag, same-room rule, maintenance linked correctly. |
| **M7** | Dashboards | Reporter / Staff / Admin dashboards (`DashboardService`). | Counts verified against seeded data. |
| **M8** | Search, filters & reports | Filters on all lists, date-range reports (issues, resolution time, maintenance cost). | Tests: date-range boundaries (inclusive, UTC↔Dhaka). |
| **M9** | Hardening & docs | Global error handling and status pages, logging review, security checklist pass, README (setup, secrets, migrations, screenshots). | Full test run, manual security checks (IDOR, CSRF, over-posting). |

---

## 11. Ambiguities, Missing Requirements & Risks

### Resolved by your answers
Account creation, team-based assignment, project name kept, test stack, visibility (own + public list), close/reopen flow, **no attachments**, no email-domain restriction, admin-managed rooms, admin sets priority, no faculty approval, department = profile only, staff links assets, edit/cancel only while Submitted, internal comments.

### Remaining ambiguities
- Does the public open-issue list show the full description, or only title/category/location/status?
- Should a Support Staff member see *all* issues read-only, or only their team's (proposed: team only)?
- Should a reporter be able to comment after their issue is Closed (proposed: no)?

### Technical risks

| Risk | Impact | Mitigation |
|---|---|---|
| `&` in the project/folder name | CLI and MSBuild quoting problems, odd URL encoding in tools | Always quote paths. Use the VS Package Manager Console for migrations. Test project named without `&`. |
| No `.gitignore` yet | Build output and `.vs` user files committed | First step of M0 |
| Self-registration without verification | Anyone can sign up as "Faculty" or create fake accounts | Accepted for an academic project. Admin can deactivate. Documented as a known limitation. |
| SQL Express connection setup (instance name, certificate) | App can't connect | `TrustServerCertificate=True` for local dev, documented in README |
| Role change while logged in | User's cookie keeps the old role | Security-stamp validation interval set low. Admin is told the user must log in again. |
| SQLite tests vs SQL Server behaviour | Some differences (rowversion, collation) | Keep tests on business rules. Verify concurrency manually on SQL Server. |
| Time zones in reports | Off-by-one-day errors | Store UTC. Convert Dhaka date ranges to UTC in one helper. Unit-test the boundaries. |
| Scope creep (notifications, SLA, uploads) | Delays | Out-of-scope list in section 1. New ideas go to a Release 2 backlog. |

---

## 12. Verification Approach (whole project)
- `dotnet build` and `dotnet test` (on `Smart_AIUB_Service_Issue.Tests`) are run and their real output reported after each milestone.
- Migrations are applied to local SQL Server 2022 Express and checked in SSMS (tables, FKs, indexes).
- Manual browser walkthrough per milestone with the four seeded roles, including negative checks (wrong role → 403, other user's issue id → 403/404).

---

## 13. Decisions Needing Your Approval (before database design & implementation)

| # | Decision | My recommendation |
|---|---|---|
| D1 | Add a short **Title** field to the issue form? | Yes (100 chars). Otherwise auto-title "Category – Room". |
| D2 | Status list and transition table in section 5 | Approve as written |
| D3 | Reopen window | 7 days, configurable |
| D4 | Category → **Default Support Team** pre-fill | Yes |
| D5 | Ticket number shown as `ISS-000123` derived from Id | Yes |
| D6 | Store enums as strings | Yes |
| D7 | Deactivate instead of delete for master data and users | Yes |
| D8 | Custom Account controller/views instead of scaffolded Identity UI | Yes |
| D9 | Can Support Staff and Admin also *report* issues? | Yes |
| D10 | Can Support Staff create/edit assets, or only Admin? | Admin only. Staff add maintenance records. |
| D11 | Linked asset must be in the same room as the issue | Yes |
| D12 | Public list shows Title, Category, Building/Room, Status, Date (no description, no reporter) | Yes |
| D13 | Support Staff see only their team's issues (plus public list) | Yes |
| D14 | Store UTC, display Asia/Dhaka | Yes |
| D15 | Seed admin account with email/password from User Secrets | Yes |
| D16 | Accept the self-registration risk (anyone can pick Faculty) for Release 1 | Yes, documented |
| D17 | Confirm out-of-scope list: attachments, notifications, SLA/auto-close, export, Supervisor | Confirm |

After you approve this plan (with any changes), the next step is **M0** (repo hygiene and packages), then **M1a**, the detailed database design for your review. No migration is created before that review.
