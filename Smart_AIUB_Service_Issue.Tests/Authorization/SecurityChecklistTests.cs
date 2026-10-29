using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Controllers;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Account;
using Smart_AIUB_Service_Issue.ViewModels.Admin;
using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.Tests.Authorization;

/// <summary>
/// Automated parts of the security checklist (Plan/SECURITY.md): CSRF and over-posting.
/// IDOR is covered by IssueAuthorizationTests; role checks by AdminControllerAuthorizationTests.
/// </summary>
public class SecurityChecklistTests
{
    private static readonly Type[] Controllers = typeof(HomeController).Assembly.GetTypes()
        .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
        .ToArray();

    private static IEnumerable<MethodInfo> Actions(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    // ---------- CSRF ----------

    [Fact]
    public void Only_the_read_only_error_page_skips_anti_forgery_checks()
    {
        // Program.cs adds AutoValidateAntiforgeryToken for every POST; [IgnoreAntiforgeryToken]
        // anywhere else would silently turn that off. The single exception is the error page,
        // which only shows text (see HomeController.HttpError).
        var offenders = Controllers
            .Where(c => c.GetCustomAttribute<IgnoreAntiforgeryTokenAttribute>() is not null)
            .Select(c => c.Name)
            .Concat(Controllers.SelectMany(Actions)
                .Where(a => a.GetCustomAttribute<IgnoreAntiforgeryTokenAttribute>() is not null)
                .Select(a => $"{a.DeclaringType!.Name}.{a.Name}"));

        Assert.Equal(["HomeController.HttpError"], offenders);
    }

    [Fact]
    public void Only_login_and_register_accept_anonymous_posts()
    {
        var anonymousPosts = Controllers.SelectMany(Actions)
            .Where(a => a.GetCustomAttribute<HttpPostAttribute>() is not null)
            .Where(a => a.GetCustomAttribute<AllowAnonymousAttribute>() is not null ||
                        a.DeclaringType!.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(a => $"{a.DeclaringType!.Name}.{a.Name}")
            .Distinct()
            .Order();

        Assert.Equal(["AccountController.Login", "AccountController.Register"], anonymousPosts);
    }

    // ---------- Over-posting ----------

    [Theory]
    [InlineData(typeof(IssueFormViewModel), new[] { "Status", "Priority", "ReporterId", "AssignedTeamId", "AssignedStaffId", "ResolutionNote", "AssetId" })]
    [InlineData(typeof(RegisterViewModel), new[] { "IsActive", "SupportTeamId", "EmailConfirmed", "Id" })]
    [InlineData(typeof(CreateUserViewModel), new[] { "IsActive", "EmailConfirmed", "Id" })]
    [InlineData(typeof(AddCommentViewModel), new[] { "AuthorId", "IssueId", "CreatedAt" })]
    public void Form_models_have_no_fields_a_user_must_not_set(Type formModel, string[] forbidden)
    {
        // Forms bind to these ViewModels, never to entities. A field that does not exist
        // cannot be smuggled in with a hand-made request.
        var properties = formModel.GetProperties().Select(p => p.Name);

        Assert.Empty(properties.Intersect(forbidden));
    }

    [Fact]
    public async Task Posted_role_on_the_edit_user_form_is_ignored()
    {
        await using var services = await TestServices.CreateAsync(s => s.AddScoped<IUserManagementService, UserManagementService>());
        var userManager = services.Get<UserManager<ApplicationUser>>();
        var db = services.Get<ApplicationDbContext>();
        var department = new Department { Code = "CSE", Name = "CSE" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        var student = new ApplicationUser
        {
            UserName = "s@test.com", Email = "s@test.com", FullName = "Student",
            DepartmentId = department.Id, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        await userManager.CreateAsync(student, "Password1");
        await userManager.AddToRoleAsync(student, Roles.Student);

        // A tampered form that tries to turn the student into an admin.
        var result = await services.Get<IUserManagementService>().UpdateAsync(new EditUserViewModel
        {
            Id = student.Id, FullName = "Student", DepartmentId = department.Id, Role = Roles.Admin
        });

        Assert.True(result.Succeeded, result.Error);
        var roles = await userManager.GetRolesAsync((await userManager.FindByIdAsync(student.Id))!);
        Assert.Equal([Roles.Student], roles);
    }
}
