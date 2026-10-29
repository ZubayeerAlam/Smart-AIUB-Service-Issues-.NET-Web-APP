using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Account;

namespace Smart_AIUB_Service_Issue.Tests.Services;

public class AccountServiceTests
{
    private static async Task<(TestServices Services, int DepartmentId)> SetUpAsync()
    {
        var services = await TestServices.CreateAsync(s => s.AddScoped<IAccountService, AccountService>());

        var db = services.Get<ApplicationDbContext>();
        var department = new Department { Code = "CSE", Name = "Computer Science & Engineering" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        return (services, department.Id);
    }

    private static RegisterViewModel ValidModel(int departmentId, string role = Roles.Student) => new()
    {
        FullName = "Test Student",
        Email = "student@example.com",
        UniversityId = "22-12345-1",
        Role = role,
        DepartmentId = departmentId,
        Password = "Password1",
        ConfirmPassword = "Password1"
    };

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.SupportStaff)]
    [InlineData("admin")]
    [InlineData("")]
    public async Task Register_rejects_roles_that_are_not_self_registrable(string role)
    {
        var (services, departmentId) = await SetUpAsync();
        await using var _ = services;

        var (result, user) = await services.Get<IAccountService>().RegisterAsync(ValidModel(departmentId, role));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "InvalidRole");
        Assert.Null(user);
        Assert.Empty(services.Get<ApplicationDbContext>().Users);
    }

    [Theory]
    [InlineData(Roles.Student)]
    [InlineData(Roles.Faculty)]
    public async Task Register_creates_active_user_with_the_chosen_role(string role)
    {
        var (services, departmentId) = await SetUpAsync();
        await using var _ = services;

        var (result, user) = await services.Get<IAccountService>().RegisterAsync(ValidModel(departmentId, role));

        Assert.True(result.Succeeded);
        Assert.NotNull(user);
        Assert.True(user.IsActive);
        Assert.Equal(departmentId, user.DepartmentId);
        Assert.True(await services.Get<UserManager<ApplicationUser>>().IsInRoleAsync(user, role));
    }

    [Fact]
    public async Task Register_rejects_inactive_department()
    {
        var (services, departmentId) = await SetUpAsync();
        await using var _ = services;

        var db = services.Get<ApplicationDbContext>();
        (await db.Departments.FindAsync(departmentId))!.IsActive = false;
        await db.SaveChangesAsync();

        var (result, _) = await services.Get<IAccountService>().RegisterAsync(ValidModel(departmentId));

        Assert.Contains(result.Errors, e => e.Code == "InvalidDepartment");
    }

    [Fact]
    public async Task Register_rejects_duplicate_university_id()
    {
        var (services, departmentId) = await SetUpAsync();
        await using var _ = services;
        var accountService = services.Get<IAccountService>();
        await accountService.RegisterAsync(ValidModel(departmentId));

        var second = ValidModel(departmentId);
        second.Email = "someone.else@example.com";
        var (result, _) = await accountService.RegisterAsync(second);

        Assert.Contains(result.Errors, e => e.Code == "DuplicateUniversityId");
    }

    [Fact]
    public async Task Register_rejects_weak_password_and_saves_nothing()
    {
        var (services, departmentId) = await SetUpAsync();
        await using var _ = services;
        var model = ValidModel(departmentId);
        model.Password = model.ConfirmPassword = "password"; // no digit, no uppercase

        var (result, user) = await services.Get<IAccountService>().RegisterAsync(model);

        Assert.False(result.Succeeded);
        Assert.Null(user);
        Assert.Empty(services.Get<ApplicationDbContext>().Users);
    }
}
