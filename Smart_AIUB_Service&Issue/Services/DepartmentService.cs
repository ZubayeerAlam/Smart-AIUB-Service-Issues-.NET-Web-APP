using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _db;

    public DepartmentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<Department>> GetPageAsync(string? search, string? status, int page)
    {
        var query = _db.Departments.AsNoTracking().WhereStatus(status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.Code.Contains(term) || d.Name.Contains(term));
        }

        return query.OrderBy(d => d.Name).ToPagedResultAsync(page);
    }

    public Task<DepartmentFormViewModel?> GetFormAsync(int id) =>
        _db.Departments.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DepartmentFormViewModel { Id = d.Id, Code = d.Code, Name = d.Name })
            .FirstOrDefaultAsync();

    public async Task<ServiceResult> SaveAsync(DepartmentFormViewModel model)
    {
        var code = model.Code.Trim().ToUpperInvariant();
        var name = model.Name.Trim();

        // Friendly check first; the unique index in the database is the final safety net.
        if (await _db.Departments.AnyAsync(d => d.Id != model.Id && d.Code == code))
        {
            return ServiceResult.Fail($"Code '{code}' is already used by another department.", nameof(model.Code));
        }
        if (await _db.Departments.AnyAsync(d => d.Id != model.Id && d.Name == name))
        {
            return ServiceResult.Fail($"A department named '{name}' already exists.", nameof(model.Name));
        }

        Department? department;
        if (model.Id == 0)
        {
            department = new Department();
            _db.Departments.Add(department);
        }
        else
        {
            department = await _db.Departments.FindAsync(model.Id);
            if (department is null)
            {
                return ServiceResult.Fail("Department not found.");
            }
        }

        department.Code = code;
        department.Name = name;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive) =>
        await _db.SetActiveAsync<Department>(id, isActive)
            ? ServiceResult.Success()
            : ServiceResult.Fail("Department not found.");

    public Task<List<SelectListItem>> GetOptionsAsync(int? includeId = null) =>
        _db.Departments.AsNoTracking()
            .Where(d => d.IsActive || d.Id == includeId)
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem(d.IsActive ? $"{d.Name} ({d.Code})" : $"{d.Name} ({d.Code}) - inactive", d.Id.ToString()))
            .ToListAsync();
}
