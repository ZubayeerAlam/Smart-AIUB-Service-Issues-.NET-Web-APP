using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services;

public class BuildingService : IBuildingService
{
    private readonly ApplicationDbContext _db;

    public BuildingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<BuildingListItem>> GetPageAsync(string? search, string? status, int page)
    {
        var query = _db.Buildings.AsNoTracking().WhereStatus(status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b => b.Code.Contains(term) || b.Name.Contains(term));
        }

        return query
            .OrderBy(b => b.Name)
            .Select(b => new BuildingListItem(b.Id, b.Code, b.Name, b.Rooms.Count, b.IsActive))
            .ToPagedResultAsync(page);
    }

    public Task<BuildingFormViewModel?> GetFormAsync(int id) =>
        _db.Buildings.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BuildingFormViewModel { Id = b.Id, Code = b.Code, Name = b.Name })
            .FirstOrDefaultAsync();

    public async Task<ServiceResult> SaveAsync(BuildingFormViewModel model)
    {
        var code = model.Code.Trim().ToUpperInvariant();
        var name = model.Name.Trim();

        if (await _db.Buildings.AnyAsync(b => b.Id != model.Id && b.Code == code))
        {
            return ServiceResult.Fail($"Code '{code}' is already used by another building.", nameof(model.Code));
        }
        if (await _db.Buildings.AnyAsync(b => b.Id != model.Id && b.Name == name))
        {
            return ServiceResult.Fail($"A building named '{name}' already exists.", nameof(model.Name));
        }

        Building? building;
        if (model.Id == 0)
        {
            building = new Building();
            _db.Buildings.Add(building);
        }
        else
        {
            building = await _db.Buildings.FindAsync(model.Id);
            if (building is null)
            {
                return ServiceResult.Fail("Building not found.");
            }
        }

        building.Code = code;
        building.Name = name;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive) =>
        await _db.SetActiveAsync<Building>(id, isActive)
            ? ServiceResult.Success()
            : ServiceResult.Fail("Building not found.");

    public Task<List<SelectListItem>> GetOptionsAsync(int? includeId = null) =>
        _db.Buildings.AsNoTracking()
            .Where(b => b.IsActive || b.Id == includeId)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.IsActive ? b.Name : b.Name + " - inactive", b.Id.ToString()))
            .ToListAsync();
}
