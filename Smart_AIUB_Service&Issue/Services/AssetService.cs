using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Assets;

namespace Smart_AIUB_Service_Issue.Services;

public class AssetService : IAssetService
{
    private readonly ApplicationDbContext _db;
    private readonly IIssueService _issues;
    private readonly IAssetTypeService _types;

    public AssetService(ApplicationDbContext db, IIssueService issues, IAssetTypeService types)
    {
        _db = db;
        _issues = issues;
        _types = types;
    }

    public Task<PagedResult<AssetListItem>> GetPageAsync(string? search, int? typeId, int? buildingId, AssetStatus? status, int page)
    {
        var query = _db.Assets.AsNoTracking();

        if (typeId is not null) query = query.Where(a => a.AssetTypeId == typeId);
        if (buildingId is not null) query = query.Where(a => a.Room.BuildingId == buildingId);
        if (status is not null) query = query.Where(a => a.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a =>
                a.AssetTag.Contains(term) || a.Name.Contains(term) ||
                (a.SerialNumber != null && a.SerialNumber.Contains(term)) ||
                a.Room.RoomNumber.Contains(term));
        }

        return query
            .OrderBy(a => a.AssetTag)
            .Select(a => new AssetListItem(
                a.Id, a.AssetTag, a.Name, a.AssetType.Name, a.Room.Building.Name, a.Room.RoomNumber, a.Status,
                a.MaintenanceRecords.Max(m => (DateTime?)m.PerformedAt)))
            .ToPagedResultAsync(page);
    }

    public Task<AssetFormViewModel?> GetFormAsync(int id) =>
        _db.Assets.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AssetFormViewModel
            {
                Id = a.Id,
                AssetTag = a.AssetTag,
                Name = a.Name,
                AssetTypeId = a.AssetTypeId,
                BuildingId = a.Room.BuildingId,
                RoomId = a.RoomId,
                SerialNumber = a.SerialNumber,
                Status = a.Status,
                PurchaseDate = a.PurchaseDate,
                WarrantyExpiry = a.WarrantyExpiry,
                Notes = a.Notes
            })
            .FirstOrDefaultAsync();

    public async Task FillFormOptionsAsync(AssetFormViewModel model)
    {
        model.TypeOptions = await _types.GetOptionsAsync(model.AssetTypeId);

        // Same building/room lists as the issue form (active only).
        var issueForm = new ViewModels.Issues.IssueFormViewModel { BuildingId = model.BuildingId };
        await _issues.FillFormOptionsAsync(issueForm);
        model.BuildingOptions = issueForm.BuildingOptions;
        model.RoomOptions = issueForm.RoomOptions;
    }

    public async Task<ServiceResult<int>> SaveAsync(AssetFormViewModel model)
    {
        var tag = model.AssetTag.Trim().ToUpperInvariant();

        Asset? asset = null;
        if (model.Id != 0)
        {
            asset = await _db.Assets.FindAsync(model.Id);
            if (asset is null)
            {
                return ServiceResult<int>.Fail("Asset not found.");
            }
        }

        // Friendly check first; the unique index in the database is the final safety net.
        if (await _db.Assets.AnyAsync(a => a.Id != model.Id && a.AssetTag == tag))
        {
            return ServiceResult<int>.Fail($"Asset tag '{tag}' is already used by another asset.", nameof(model.AssetTag));
        }

        // A new type must be active; keeping the current (now inactive) one is allowed.
        if (asset?.AssetTypeId != model.AssetTypeId &&
            !await _db.AssetTypes.AnyAsync(t => t.Id == model.AssetTypeId && t.IsActive))
        {
            return ServiceResult<int>.Fail("Please select an active asset type.", nameof(model.AssetTypeId));
        }

        var room = await _db.Rooms.AsNoTracking()
            .Where(r => r.Id == model.RoomId)
            .Select(r => new { r.BuildingId, Active = r.IsActive && r.Building.IsActive })
            .FirstOrDefaultAsync();
        if (room is null || room.BuildingId != model.BuildingId)
        {
            return ServiceResult<int>.Fail("The selected room is not in the selected building.", nameof(model.RoomId));
        }
        if (!room.Active && asset?.RoomId != model.RoomId)
        {
            return ServiceResult<int>.Fail("Please select an active room.", nameof(model.RoomId));
        }

        var now = DateTime.UtcNow;
        if (asset is null)
        {
            asset = new Asset { CreatedAt = now };
            _db.Assets.Add(asset);
        }

        asset.AssetTag = tag;
        asset.Name = model.Name.Trim();
        asset.AssetTypeId = model.AssetTypeId!.Value;
        asset.RoomId = model.RoomId!.Value;
        asset.SerialNumber = string.IsNullOrWhiteSpace(model.SerialNumber) ? null : model.SerialNumber.Trim();
        asset.Status = model.Status;
        asset.PurchaseDate = model.PurchaseDate;
        asset.WarrantyExpiry = model.WarrantyExpiry;
        asset.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        asset.UpdatedAt = now;

        await _db.SaveChangesAsync();
        return ServiceResult<int>.Success(asset.Id);
    }

    public async Task<AssetDetailsViewModel?> GetDetailsAsync(int id)
    {
        var model = await _db.Assets.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AssetDetailsViewModel
            {
                Id = a.Id,
                AssetTag = a.AssetTag,
                Name = a.Name,
                TypeName = a.AssetType.Name,
                BuildingName = a.Room.Building.Name,
                Floor = a.Room.Floor,
                RoomNumber = a.Room.RoomNumber,
                SerialNumber = a.SerialNumber,
                Status = a.Status,
                PurchaseDate = a.PurchaseDate,
                WarrantyExpiry = a.WarrantyExpiry,
                Notes = a.Notes
            })
            .FirstOrDefaultAsync();

        if (model is null)
        {
            return null;
        }

        model.Maintenance = await _db.MaintenanceRecords.AsNoTracking()
            .Where(m => m.AssetId == id)
            .OrderByDescending(m => m.PerformedAt).ThenByDescending(m => m.Id)
            .Select(m => new MaintenanceRecordItem(
                m.Id, m.Type, m.Description, m.PerformedAt, m.PerformedBy.FullName, m.Cost, m.IssueId))
            .ToListAsync();

        model.Issues = await _db.Issues.AsNoTracking()
            .Where(i => i.AssetId == id)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new LinkedIssueItem(i.Id, i.Title, i.Status, i.CreatedAt))
            .ToListAsync();

        return model;
    }

    public Task<List<SelectListItem>> GetRoomAssetOptionsAsync(int roomId) =>
        _db.Assets.AsNoTracking()
            .Where(a => a.RoomId == roomId && a.Status != AssetStatus.Retired)
            .OrderBy(a => a.AssetTag)
            .Select(a => new SelectListItem(a.AssetTag + " · " + a.Name, a.Id.ToString()))
            .ToListAsync();
}
