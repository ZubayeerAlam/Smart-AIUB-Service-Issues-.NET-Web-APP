using Microsoft.Extensions.DependencyInjection;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Tests.Services;

public class RoomServiceTests
{
    private static async Task<(TestServices Services, Building D, Building C)> SetUpAsync()
    {
        var services = await TestServices.CreateAsync(s => s.AddScoped<IRoomService, RoomService>());
        var db = services.Get<ApplicationDbContext>();
        var d = new Building { Code = "D", Name = "D-Building" };
        var c = new Building { Code = "C", Name = "C-Building" };
        db.Buildings.AddRange(d, c);
        await db.SaveChangesAsync();
        return (services, d, c);
    }

    [Fact]
    public async Task Save_rejects_same_room_number_twice_in_one_building()
    {
        var (services, d, _) = await SetUpAsync();
        await using var _s = services;
        var rooms = services.Get<IRoomService>();
        await rooms.SaveAsync(new RoomFormViewModel { BuildingId = d.Id, Floor = 3, RoomNumber = "301" });

        var result = await rooms.SaveAsync(new RoomFormViewModel { BuildingId = d.Id, Floor = 3, RoomNumber = "301" });

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(RoomFormViewModel.RoomNumber), result.Field);
    }

    [Fact]
    public async Task Save_allows_same_room_number_in_another_building()
    {
        var (services, d, c) = await SetUpAsync();
        await using var _s = services;
        var rooms = services.Get<IRoomService>();
        await rooms.SaveAsync(new RoomFormViewModel { BuildingId = d.Id, Floor = 3, RoomNumber = "301" });

        var result = await rooms.SaveAsync(new RoomFormViewModel { BuildingId = c.Id, Floor = 3, RoomNumber = "301" });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Save_rejects_new_room_in_inactive_building()
    {
        var (services, d, _) = await SetUpAsync();
        await using var _s = services;
        var db = services.Get<ApplicationDbContext>();
        d.IsActive = false;
        await db.SaveChangesAsync();

        var result = await services.Get<IRoomService>()
            .SaveAsync(new RoomFormViewModel { BuildingId = d.Id, Floor = 1, RoomNumber = "101" });

        Assert.Equal(nameof(RoomFormViewModel.BuildingId), result.Field);
    }

    [Fact]
    public async Task Save_can_still_edit_a_room_whose_building_was_deactivated()
    {
        var (services, d, _) = await SetUpAsync();
        await using var _s = services;
        var rooms = services.Get<IRoomService>();
        await rooms.SaveAsync(new RoomFormViewModel { BuildingId = d.Id, Floor = 1, RoomNumber = "101" });
        var db = services.Get<ApplicationDbContext>();
        d.IsActive = false;
        await db.SaveChangesAsync();
        var roomId = db.Rooms.Single().Id;

        var result = await rooms.SaveAsync(
            new RoomFormViewModel { Id = roomId, BuildingId = d.Id, Floor = 1, RoomNumber = "101", Name = "Seminar room" });

        Assert.True(result.Succeeded);
    }
}
