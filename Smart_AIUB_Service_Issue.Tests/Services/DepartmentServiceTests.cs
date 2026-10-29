using Microsoft.Extensions.DependencyInjection;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Tests.Services;

/// <summary>
/// Departments are the simplest lookup, so they also cover the shared helpers:
/// paging, the status filter and activate/deactivate.
/// </summary>
public class DepartmentServiceTests
{
    private static Task<TestServices> CreateAsync() =>
        TestServices.CreateAsync(s => s.AddScoped<IDepartmentService, DepartmentService>());

    [Fact]
    public async Task Save_creates_department_with_uppercase_trimmed_code()
    {
        await using var services = await CreateAsync();

        var result = await services.Get<IDepartmentService>()
            .SaveAsync(new DepartmentFormViewModel { Code = " cse ", Name = " Computer Science " });

        Assert.True(result.Succeeded);
        var saved = Assert.Single(services.Get<ApplicationDbContext>().Departments);
        Assert.Equal("CSE", saved.Code);
        Assert.Equal("Computer Science", saved.Name);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Save_rejects_duplicate_code_even_with_different_case()
    {
        await using var services = await CreateAsync();
        var departments = services.Get<IDepartmentService>();
        await departments.SaveAsync(new DepartmentFormViewModel { Code = "CSE", Name = "Computer Science" });

        var result = await departments.SaveAsync(new DepartmentFormViewModel { Code = "cse", Name = "Something else" });

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(DepartmentFormViewModel.Code), result.Field);
    }

    [Fact]
    public async Task Save_rejects_duplicate_name()
    {
        await using var services = await CreateAsync();
        var departments = services.Get<IDepartmentService>();
        await departments.SaveAsync(new DepartmentFormViewModel { Code = "CSE", Name = "Computer Science" });

        var result = await departments.SaveAsync(new DepartmentFormViewModel { Code = "CS", Name = "Computer Science" });

        Assert.Equal(nameof(DepartmentFormViewModel.Name), result.Field);
    }

    [Fact]
    public async Task Save_allows_editing_a_department_without_changing_its_code()
    {
        await using var services = await CreateAsync();
        var departments = services.Get<IDepartmentService>();
        await departments.SaveAsync(new DepartmentFormViewModel { Code = "CSE", Name = "Old name" });
        var id = services.Get<ApplicationDbContext>().Departments.Single().Id;

        // Same code as itself is not a "duplicate".
        var result = await departments.SaveAsync(new DepartmentFormViewModel { Id = id, Code = "CSE", Name = "New name" });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SetActive_deactivates_and_status_filter_hides_it()
    {
        await using var services = await CreateAsync();
        var departments = services.Get<IDepartmentService>();
        await departments.SaveAsync(new DepartmentFormViewModel { Code = "CSE", Name = "Computer Science" });
        await departments.SaveAsync(new DepartmentFormViewModel { Code = "EEE", Name = "Electrical" });
        var cse = services.Get<ApplicationDbContext>().Departments.Single(d => d.Code == "CSE");

        var result = await departments.SetActiveAsync(cse.Id, isActive: false);

        Assert.True(result.Succeeded);
        var active = await departments.GetPageAsync(null, StatusFilter.Active, 1);
        var inactive = await departments.GetPageAsync(null, StatusFilter.Inactive, 1);
        Assert.Equal("EEE", Assert.Single(active.Items).Code);
        Assert.Equal("CSE", Assert.Single(inactive.Items).Code);
        // Deactivated departments are no longer offered in dropdowns.
        Assert.Single(await departments.GetOptionsAsync());
    }

    [Fact]
    public async Task GetPage_splits_into_pages_and_clamps_out_of_range_page()
    {
        await using var services = await CreateAsync();
        var db = services.Get<ApplicationDbContext>();
        for (var i = 1; i <= 25; i++)
        {
            db.Departments.Add(new Department { Code = $"D{i:00}", Name = $"Department {i:00}" });
        }
        await db.SaveChangesAsync();
        var departments = services.Get<IDepartmentService>();

        var page3 = await departments.GetPageAsync(null, null, 3);
        var page99 = await departments.GetPageAsync(null, null, 99);

        Assert.Equal(3, page3.TotalPages);
        Assert.Equal(25, page3.TotalCount);
        Assert.Equal(5, page3.Items.Count);
        Assert.False(page3.HasNext);
        Assert.Equal(3, page99.Page); // clamped to the last page
    }
}
