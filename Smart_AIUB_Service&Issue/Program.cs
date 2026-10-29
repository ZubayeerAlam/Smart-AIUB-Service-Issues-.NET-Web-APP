using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Data.Seed;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services;
using Smart_AIUB_Service_Issue.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// The connection string lives in User Secrets (development), not in appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("AIUB_Service&IssuesDbContext")
    ?? throw new InvalidOperationException(
        "Connection string 'AIUB_Service&IssuesDbContext' not found. " +
        "Set it with: dotnet user-secrets set \"ConnectionStrings:AIUB_Service&IssuesDbContext\" \"<connection string>\"");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Shows a helpful page for database/migration errors (Development only).
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ASP.NET Core Identity: users, roles, password hashing, lockout.
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(IdentityConfig.Configure) // password/lockout rules
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;                       // JavaScript cannot read the cookie
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Every 3 minutes the login cookie is re-checked against the database.
// When an Admin deactivates a user (or changes their role) we update the user's
// security stamp, so their old cookie stops working within about 3 minutes.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(3));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin))
    .AddPolicy(Policies.StaffOrAdmin, policy => policy.RequireRole(Roles.SupportStaff, Roles.Admin))
    // Secure by default: every page needs a logged-in user unless it is marked [AllowAnonymous].
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Services (business logic). Scoped = one instance per web request, sharing its DbContext.
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<ISupportTeamService, SupportTeamService>();
builder.Services.AddScoped<IIssueCategoryService, IssueCategoryService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.Configure<IssueOptions>(builder.Configuration.GetSection(IssueOptions.SectionName));
builder.Services.AddScoped<IIssueActorService, IssueActorService>();
builder.Services.AddScoped<IIssueService, IssueService>();
builder.Services.AddScoped<IIssueWorkflowService, IssueWorkflowService>();
builder.Services.AddScoped<IAssetTypeService, AssetTypeService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Resource-based authorization for single issues (see IssueAuthorizationHandler).
builder.Services.AddScoped<IAuthorizationHandler, IssueAuthorizationHandler>();

builder.Services.AddControllersWithViews(options =>
{
    // Checks the anti-forgery token on every POST/PUT/DELETE (CSRF protection).
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// Creates roles, the first Admin account and sample lookup data (only if missing).
await DbSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // who are you? (reads the login cookie)
app.UseAuthorization();  // are you allowed? (roles/policies)

// CSS/JS/images must load on the login page too, before anyone is logged in.
app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
