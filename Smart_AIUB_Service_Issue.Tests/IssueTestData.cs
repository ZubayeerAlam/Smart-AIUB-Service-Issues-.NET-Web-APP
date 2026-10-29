using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.Tests;

/// <summary>
/// A small campus for issue tests: two buildings with rooms, a category, two teams,
/// two reporters, staff in each team and an admin.
/// </summary>
public sealed class IssueTestData : IAsyncDisposable
{
    public TestServices Services { get; }
    public Building D { get; private set; } = null!;
    public Building C { get; private set; } = null!;
    public Room D301 { get; private set; } = null!;
    public Room C101 { get; private set; } = null!;
    public IssueCategory Projector { get; private set; } = null!;
    public AssetType ProjectorType { get; private set; } = null!;
    public SupportTeam ItTeam { get; private set; } = null!;
    public SupportTeam ElectricalTeam { get; private set; } = null!;
    public ApplicationUser Reporter { get; private set; } = null!;
    public ApplicationUser OtherReporter { get; private set; } = null!;
    public ApplicationUser ItStaff { get; private set; } = null!;
    public ApplicationUser ElectricalStaff { get; private set; } = null!;
    public ApplicationUser Admin { get; private set; } = null!;

    private IssueTestData(TestServices services)
    {
        Services = services;
    }

    public static async Task<IssueTestData> CreateAsync()
    {
        var services = await TestServices.CreateAsync(s =>
        {
            s.AddAuthorizationCore();
            s.AddScoped<IAuthorizationHandler, IssueAuthorizationHandler>();
            s.AddScoped<IIssueActorService, IssueActorService>();
            s.AddScoped<IIssueService, IssueService>();
            s.AddScoped<IIssueWorkflowService, IssueWorkflowService>();
            s.AddScoped<IAssetTypeService, AssetTypeService>();
            s.AddScoped<IAssetService, AssetService>();
            s.AddScoped<IMaintenanceService, MaintenanceService>();
        });

        var data = new IssueTestData(services);
        await data.SeedAsync();
        return data;
    }

    private async Task SeedAsync()
    {
        var db = Services.Get<ApplicationDbContext>();

        D = new Building { Code = "D", Name = "D-Building" };
        C = new Building { Code = "C", Name = "C-Building" };
        D301 = new Room { Building = D, Floor = 3, RoomNumber = "301" };
        C101 = new Room { Building = C, Floor = 1, RoomNumber = "101" };
        ItTeam = new SupportTeam { Name = "IT Support" };
        ElectricalTeam = new SupportTeam { Name = "Electrical" };
        Projector = new IssueCategory { Name = "Projector", DefaultSupportTeam = ItTeam };
        ProjectorType = new AssetType { Name = "Projector" };
        db.AddRange(D, C, D301, C101, ItTeam, ElectricalTeam, Projector, ProjectorType);
        await db.SaveChangesAsync();

        Reporter = await CreateUserAsync("reporter@test.com", Roles.Student);
        OtherReporter = await CreateUserAsync("other@test.com", Roles.Faculty);
        ItStaff = await CreateUserAsync("it@test.com", Roles.SupportStaff, ItTeam.Id);
        ElectricalStaff = await CreateUserAsync("elec@test.com", Roles.SupportStaff, ElectricalTeam.Id);
        Admin = await CreateUserAsync("admin@test.com", Roles.Admin);
    }

    private async Task<ApplicationUser> CreateUserAsync(string email, string role, int? teamId = null)
    {
        var userManager = Services.Get<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email, Email = email, FullName = email.Split('@')[0],
            SupportTeamId = teamId, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        Assert.True((await userManager.CreateAsync(user, "Password1")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
        return user;
    }

    /// <summary>A logged-in user as the web app would see them (id + role claims).</summary>
    public static ClaimsPrincipal PrincipalFor(ApplicationUser user, string role) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Role, role)],
            authenticationType: "Test"));

    public IssueActor ActorFor(ApplicationUser user, string role) =>
        new(user.Id, role == Roles.Admin, role == Roles.SupportStaff, user.SupportTeamId);

    public IssueFormViewModel ValidForm(Room? room = null) => new()
    {
        CategoryId = Projector.Id,
        BuildingId = (room ?? D301).BuildingId,
        RoomId = (room ?? D301).Id,
        Title = "Projector not working",
        Description = "The projector in this room does not turn on."
    };

    /// <summary>Reports an issue as <see cref="Reporter"/> and returns its id.</summary>
    public async Task<int> ReportAsync()
    {
        var result = await Services.Get<IIssueService>().ReportAsync(ValidForm(), Reporter.Id);
        Assert.True(result.Succeeded, result.Error);
        return result.Value;
    }

    public ViewModels.Assets.AssetFormViewModel AssetForm(string tag, Room? room = null) => new()
    {
        AssetTag = tag,
        Name = "Test projector",
        AssetTypeId = ProjectorType.Id,
        BuildingId = (room ?? D301).BuildingId,
        RoomId = (room ?? D301).Id
    };

    /// <summary>Creates an asset through the service and returns its id.</summary>
    public async Task<int> CreateAssetAsync(string tag, Room? room = null)
    {
        var result = await Services.Get<IAssetService>().SaveAsync(AssetForm(tag, room));
        Assert.True(result.Succeeded, result.Error);
        return result.Value;
    }

    /// <summary>Test shortcut: put an issue in a state directly, skipping the workflow.</summary>
    public async Task SetStateAsync(int issueId, Models.Enums.IssueStatus status, SupportTeam? team = null)
    {
        var db = Services.Get<ApplicationDbContext>();
        var issue = await db.Issues.FindAsync(issueId);
        issue!.Status = status;
        issue.AssignedTeamId = team?.Id;
        await db.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => Services.DisposeAsync();
}
