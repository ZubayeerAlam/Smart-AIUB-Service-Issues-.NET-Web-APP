using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Assets;

namespace Smart_AIUB_Service_Issue.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly ApplicationDbContext _db;

    public MaintenanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<MaintenanceFormViewModel?> NewFormAsync(int assetId, int? issueId)
    {
        var asset = await _db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assetId);
        if (asset is null)
        {
            return null;
        }

        var model = new MaintenanceFormViewModel
        {
            AssetId = assetId,
            IssueId = issueId,
            AssetStatus = asset.Status,
            PerformedAtLocal = DhakaTime.NowInDhaka()
        };
        await FillFormOptionsAsync(model);
        return model;
    }

    public async Task FillFormOptionsAsync(MaintenanceFormViewModel model)
    {
        model.AssetLabel = await _db.Assets.AsNoTracking()
            .Where(a => a.Id == model.AssetId)
            .Select(a => a.AssetTag + " · " + a.Name + " (" + a.Room.Building.Name + ", Room " + a.Room.RoomNumber + ")")
            .FirstOrDefaultAsync() ?? "";

        model.IssueOptions = (await _db.Issues.AsNoTracking()
                .Where(i => i.AssetId == model.AssetId)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new { i.Id, i.Title })
                .ToListAsync())
            .Select(i => new SelectListItem($"{IssueDisplay.TicketNumber(i.Id)} · {i.Title}", i.Id.ToString()));
    }

    public async Task<ServiceResult> CreateAsync(MaintenanceFormViewModel model, IssueActor actor)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == model.AssetId);
        if (asset is null)
        {
            return ServiceResult.Fail("Asset not found.");
        }

        // Only issues already linked to this asset may be referenced.
        if (model.IssueId is not null &&
            !await _db.Issues.AnyAsync(i => i.Id == model.IssueId && i.AssetId == asset.Id))
        {
            return ServiceResult.Fail("The related issue must be linked to this asset first.", nameof(model.IssueId));
        }

        var performedAtUtc = DhakaTime.FromDhakaToUtc(model.PerformedAtLocal!.Value);
        if (performedAtUtc > DateTime.UtcNow.AddMinutes(5))
        {
            return ServiceResult.Fail("The date and time cannot be in the future.", nameof(model.PerformedAtLocal));
        }

        var now = DateTime.UtcNow;
        _db.MaintenanceRecords.Add(new MaintenanceRecord
        {
            AssetId = asset.Id,
            IssueId = model.IssueId,
            PerformedById = actor.UserId,
            Type = model.Type,
            Description = model.Description.Trim(),
            PerformedAt = performedAtUtc,
            Cost = model.Cost,
            CreatedAt = now
        });

        // Record + new asset status in one save (one transaction).
        asset.Status = model.AssetStatus;
        asset.UpdatedAt = now;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }
}
