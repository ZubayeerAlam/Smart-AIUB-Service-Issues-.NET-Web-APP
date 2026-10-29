using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Controllers;

namespace Smart_AIUB_Service_Issue.Tests.Authorization;

/// <summary>
/// "A non-admin gets 403" is guaranteed by the [Authorize(Policy = AdminOnly)] attribute.
/// These tests fail the build if someone adds an admin controller or action that loses it.
/// </summary>
public class AdminControllerAuthorizationTests
{
    private static readonly Type[] AdminControllers =
    [
        typeof(UsersController),
        typeof(DepartmentsController),
        typeof(BuildingsController),
        typeof(RoomsController),
        typeof(IssueCategoriesController),
        typeof(SupportTeamsController),
        typeof(IssueReviewController),
        typeof(AssetTypesController)
    ];

    public static TheoryData<Type> AdminControllerTypes()
    {
        var data = new TheoryData<Type>();
        foreach (var type in AdminControllers)
        {
            data.Add(type);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AdminControllerTypes))]
    public void Admin_controller_requires_AdminOnly_policy(Type controller)
    {
        Assert.True(typeof(AdminControllerBase).IsAssignableFrom(controller),
            $"{controller.Name} must derive from AdminControllerBase.");

        var policies = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Select(a => a.Policy);
        Assert.Contains(Policies.AdminOnly, policies);
    }

    [Theory]
    [MemberData(nameof(AdminControllerTypes))]
    public void Admin_controller_has_no_anonymous_actions(Type controller)
    {
        var anonymousActions = controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(m => m.Name);

        Assert.Empty(anonymousActions);
    }

    [Fact]
    public void Every_non_admin_controller_is_listed_on_purpose()
    {
        // Controllers open to other roles must be added here on purpose, with their own
        // authorization tests (IssuesController: see IssueAuthorizationTests).
        var allowedNonAdmin = new[]
        {
            typeof(HomeController), typeof(AccountController), typeof(IssuesController), typeof(DashboardController),
            typeof(AssetsController), typeof(MaintenanceController) // see the StaffOrAdmin tests below
        };

        var unexpected = typeof(AdminControllerBase).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
            .Where(t => !allowedNonAdmin.Contains(t) && !AdminControllers.Contains(t))
            .Select(t => t.Name);

        Assert.Empty(unexpected);
    }

    [Theory]
    [InlineData(typeof(AssetsController))]
    [InlineData(typeof(MaintenanceController))]
    public void Asset_pages_require_staff_or_admin(Type controller)
    {
        var policies = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Select(a => a.Policy);
        Assert.Contains(Policies.StaffOrAdmin, policies);
    }

    [Theory]
    [InlineData(nameof(AssetsController.Create))]
    [InlineData(nameof(AssetsController.Edit))]
    public void Only_admins_can_add_or_edit_assets(string action)
    {
        // Every overload (GET form and POST save) must carry AdminOnly.
        var methods = typeof(AssetsController).GetMethods().Where(m => m.Name == action).ToList();

        Assert.NotEmpty(methods);
        Assert.All(methods, m =>
            Assert.Contains(Policies.AdminOnly, m.GetCustomAttributes<AuthorizeAttribute>().Select(a => a.Policy)));
    }
}
