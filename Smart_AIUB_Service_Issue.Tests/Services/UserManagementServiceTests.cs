using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Tests.Services;

public class UserManagementServiceTests
{
    private static async Task<(TestServices Services, int TeamId)> SetUpAsync()
    {
        var services = await TestServices.CreateAsync(s => s.AddScoped<IUserManagementService, UserManagementService>());
        var db = services.Get<ApplicationDbContext>();
        var team = new SupportTeam { Name = "IT Support" };
        db.SupportTeams.Add(team);
        await db.SaveChangesAsync();
        return (services, team.Id);
    }

    private static CreateUserViewModel NewStaff(int? teamId, string role = Roles.SupportStaff, string email = "tech@example.com") => new()
    {
        FullName = "Test Technician",
        Email = email,
        Role = role,
        SupportTeamId = teamId,
        Password = "Password1",
        ConfirmPassword = "Password1"
    };

    private static async Task<ApplicationUser> CreateStaffAsync(TestServices services, int teamId, string email = "tech@example.com")
    {
        var result = await services.Get<IUserManagementService>().CreateAsync(NewStaff(teamId, email: email));
        Assert.True(result.Succeeded, result.Error);
        return (await services.Get<UserManager<ApplicationUser>>().FindByEmailAsync(email))!;
    }

    [Theory]
    [InlineData(Roles.Student)]
    [InlineData(Roles.Faculty)]
    [InlineData("Superuser")]
    public async Task Create_only_allows_SupportStaff_or_Admin(string role)
    {
        var (services, teamId) = await SetUpAsync();
        await using var _ = services;

        var result = await services.Get<IUserManagementService>().CreateAsync(NewStaff(teamId, role));

        Assert.Equal(nameof(CreateUserViewModel.Role), result.Field);
    }

    [Fact]
    public async Task Create_support_staff_requires_an_active_team()
    {
        var (services, _) = await SetUpAsync();
        await using var _s = services;

        var result = await services.Get<IUserManagementService>().CreateAsync(NewStaff(teamId: null));

        Assert.Equal(nameof(CreateUserViewModel.SupportTeamId), result.Field);
    }

    [Fact]
    public async Task Create_support_staff_saves_team_and_role()
    {
        var (services, teamId) = await SetUpAsync();
        await using var _ = services;

        var user = await CreateStaffAsync(services, teamId);

        Assert.Equal(teamId, user.SupportTeamId);
        Assert.True(await services.Get<UserManager<ApplicationUser>>().IsInRoleAsync(user, Roles.SupportStaff));
    }

    [Fact]
    public async Task Admin_cannot_deactivate_own_account()
    {
        var (services, teamId) = await SetUpAsync();
        await using var _ = services;
        var me = await CreateStaffAsync(services, teamId);

        var result = await services.Get<IUserManagementService>().SetActiveAsync(me.Id, isActive: false, currentUserId: me.Id);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Deactivate_sets_IsActive_false_and_changes_security_stamp()
    {
        var (services, teamId) = await SetUpAsync();
        await using var _ = services;
        var user = await CreateStaffAsync(services, teamId);
        var stampBefore = user.SecurityStamp;

        var result = await services.Get<IUserManagementService>().SetActiveAsync(user.Id, isActive: false, currentUserId: "some-admin");

        Assert.True(result.Succeeded);
        var reloaded = await services.Get<UserManager<ApplicationUser>>().FindByIdAsync(user.Id);
        Assert.False(reloaded!.IsActive);
        // A new stamp is what signs the user out of existing sessions.
        Assert.NotEqual(stampBefore, reloaded.SecurityStamp);
    }

    [Fact]
    public async Task ResetPassword_sets_new_password_and_clears_lockout()
    {
        var (services, teamId) = await SetUpAsync();
        await using var _ = services;
        var user = await CreateStaffAsync(services, teamId);
        var userManager = services.Get<UserManager<ApplicationUser>>();
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));

        var result = await services.Get<IUserManagementService>().ResetPasswordAsync(user.Id, "NewPassword2");

        Assert.True(result.Succeeded);
        var reloaded = (await userManager.FindByIdAsync(user.Id))!;
        Assert.True(await userManager.CheckPasswordAsync(reloaded, "NewPassword2"));
        Assert.False(await userManager.IsLockedOutAsync(reloaded));
    }

    [Fact]
    public async Task ResetPassword_rejects_weak_password()
    {
        var (services, teamId) = await SetUpAsync();
        await using var _ = services;
        var user = await CreateStaffAsync(services, teamId);

        var result = await services.Get<IUserManagementService>().ResetPasswordAsync(user.Id, "weak");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task GetPage_filters_by_role()
    {
        var (services, teamId) = await SetUpAsync();
        await using var _ = services;
        await CreateStaffAsync(services, teamId, "tech1@example.com");
        await services.Get<IUserManagementService>().CreateAsync(NewStaff(null, Roles.Admin, "boss@example.com"));

        var staff = await services.Get<IUserManagementService>().GetPageAsync(null, null, Roles.SupportStaff, 1);
        var admins = await services.Get<IUserManagementService>().GetPageAsync(null, null, Roles.Admin, 1);

        Assert.Equal("tech1@example.com", Assert.Single(staff.Items).Email);
        Assert.Equal("boss@example.com", Assert.Single(admins.Items).Email);
    }
}
