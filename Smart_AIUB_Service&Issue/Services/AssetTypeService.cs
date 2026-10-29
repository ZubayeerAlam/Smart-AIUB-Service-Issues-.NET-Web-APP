using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Assets;

namespace Smart_AIUB_Service_Issue.Services;

public class AssetTypeService : IAssetTypeService
{
    private readonly ApplicationDbContext _db;

    public AssetTypeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<AssetTypeListItem>> GetPageAsync(string? search, string? status, int page)
    {
        var query = _db.AssetTypes.AsNoTracking().WhereStatus(status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => t.Name.Contains(term));
        }

        return query
            .OrderBy(t => t.Name)
            .Select(t => new AssetTypeListItem(t.Id, t.Name, _db.Assets.Count(a => a.AssetTypeId == t.Id), t.IsActive))
            .ToPagedResultAsync(page);
    }

    public Task<AssetTypeFormViewModel?> GetFormAsync(int id) =>
        _db.AssetTypes.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new AssetTypeFormViewModel { Id = t.Id, Name = t.Name })
            .FirstOrDefaultAsync();

    public async Task<ServiceResult> SaveAsync(AssetTypeFormViewModel model)
    {
        var name = model.Name.Trim();

        if (await _db.AssetTypes.AnyAsync(t => t.Id != model.Id && t.Name == name))
        {
            return ServiceResult.Fail($"An asset type named '{name}' already exists.", nameof(model.Name));
        }

        AssetType? type;
        if (model.Id == 0)
        {
            type = new AssetType();
            _db.AssetTypes.Add(type);
        }
        else
        {
            type = await _db.AssetTypes.FindAsync(model.Id);
            if (type is null)
            {
                return ServiceResult.Fail("Asset type not found.");
            }
        }

        type.Name = name;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive) =>
        await _db.SetActiveAsync<AssetType>(id, isActive)
            ? ServiceResult.Success()
            : ServiceResult.Fail("Asset type not found.");

    public Task<List<SelectListItem>> GetOptionsAsync(int? includeId = null) =>
        _db.AssetTypes.AsNoTracking()
            .Where(t => t.IsActive || t.Id == includeId)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem(t.IsActive ? t.Name : t.Name + " - inactive", t.Id.ToString()))
            .ToListAsync();
}
