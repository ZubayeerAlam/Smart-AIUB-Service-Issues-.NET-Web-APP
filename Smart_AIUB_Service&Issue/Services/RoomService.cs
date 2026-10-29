using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services;

public class RoomService : IRoomService
{
    private readonly ApplicationDbContext _db;

    public RoomService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<RoomListItem>> GetPageAsync(string? search, string? status, int? buildingId, int page)
    {
        var query = _db.Rooms.AsNoTracking().WhereStatus(status);

        if (buildingId is not null)
        {
            query = query.Where(r => r.BuildingId == buildingId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r => r.RoomNumber.Contains(term) || (r.Name != null && r.Name.Contains(term)));
        }

        return query
            .OrderBy(r => r.Building.Name).ThenBy(r => r.Floor).ThenBy(r => r.RoomNumber)
            .Select(r => new RoomListItem(
                r.Id, r.Building.Code, r.Building.Name, r.Floor, r.RoomNumber, r.Name, r.IsActive))
            .ToPagedResultAsync(page);
    }

    public Task<RoomFormViewModel?> GetFormAsync(int id) =>
        _db.Rooms.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RoomFormViewModel
            {
                Id = r.Id,
                BuildingId = r.BuildingId,
                Floor = r.Floor,
                RoomNumber = r.RoomNumber,
                Name = r.Name
            })
            .FirstOrDefaultAsync();

    public async Task<ServiceResult> SaveAsync(RoomFormViewModel model)
    {
        var roomNumber = model.RoomNumber.Trim().ToUpperInvariant();
        var name = string.IsNullOrWhiteSpace(model.Name) ? null : model.Name.Trim();

        Room? room = null;
        if (model.Id != 0)
        {
            room = await _db.Rooms.FindAsync(model.Id);
            if (room is null)
            {
                return ServiceResult.Fail("Room not found.");
            }
        }

        // A room may stay in an inactive building, but cannot be moved/added into one.
        var building = await _db.Buildings.FindAsync(model.BuildingId);
        var keepsSameBuilding = room is not null && room.BuildingId == model.BuildingId;
        if (building is null || (!building.IsActive && !keepsSameBuilding))
        {
            return ServiceResult.Fail("Please select an active building.", nameof(model.BuildingId));
        }

        if (await _db.Rooms.AnyAsync(r => r.Id != model.Id && r.BuildingId == building.Id && r.RoomNumber == roomNumber))
        {
            return ServiceResult.Fail($"Room {roomNumber} already exists in {building.Name}.", nameof(model.RoomNumber));
        }

        if (room is null)
        {
            room = new Room();
            _db.Rooms.Add(room);
        }

        room.BuildingId = building.Id;
        room.Floor = model.Floor;
        room.RoomNumber = roomNumber;
        room.Name = name;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive) =>
        await _db.SetActiveAsync<Room>(id, isActive)
            ? ServiceResult.Success()
            : ServiceResult.Fail("Room not found.");
}
