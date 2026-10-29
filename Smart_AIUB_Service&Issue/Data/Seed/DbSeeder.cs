using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Seed;

/// <summary>
/// Fills an empty database with the data the app needs to start:
/// roles, the first Admin account and sample lookup data.
/// Safe to run on every start-up: it only adds what is missing.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<ApplicationDbContext>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbSeeder));

        await SeedRolesAsync(roleManager);
        await SeedLookupDataAsync(db);
        await SeedAdminAsync(userManager, configuration, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    /// <summary>
    /// The admin email/password come from configuration (User Secrets in development):
    ///   SeedAdmin:Email, SeedAdmin:Password
    /// If they are not set, no admin is created and a warning is logged.
    /// </summary>
    private static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger logger)
    {
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SeedAdmin:Email / SeedAdmin:Password not configured. No admin account was seeded.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "System Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not create the seed admin: {errors}");
            }

            logger.LogInformation("Seed admin account {Email} created.", email);
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }

    /// <summary>
    /// Sample campus data. Each table is filled only when it is empty, so an
    /// Admin's later changes are never overwritten.
    /// </summary>
    private static async Task SeedLookupDataAsync(ApplicationDbContext db)
    {
        if (!await db.Departments.AnyAsync())
        {
            db.Departments.AddRange(
                new Department { Code = "CSE", Name = "Computer Science & Engineering" },
                new Department { Code = "EEE", Name = "Electrical & Electronic Engineering" },
                new Department { Code = "COE", Name = "Computer Engineering" },
                new Department { Code = "ARCH", Name = "Architecture" },
                new Department { Code = "BBA", Name = "Business Administration" },
                new Department { Code = "ECO", Name = "Economics" },
                new Department { Code = "ENG", Name = "English" },
                new Department { Code = "LAW", Name = "Law" },
                new Department { Code = "PHR", Name = "Pharmacy" });
        }

        if (!await db.Buildings.AnyAsync())
        {
            var buildings = new List<Building>
            {
                new() { Code = "D", Name = "D-Building" },
                new() { Code = "C", Name = "C-Building" }
            };
            for (var annex = 1; annex <= 9; annex++)
            {
                buildings.Add(new Building { Code = $"ANX-{annex}", Name = $"Annex {annex}" });
            }

            // Sample rooms: floors 1-3, three rooms per floor (101, 102, 103, 201, ...).
            // An Admin can rename, add or deactivate rooms later.
            foreach (var building in buildings)
            {
                for (var floor = 1; floor <= 3; floor++)
                {
                    for (var room = 1; room <= 3; room++)
                    {
                        building.Rooms.Add(new Room { Floor = floor, RoomNumber = $"{floor}0{room}" });
                    }
                }
            }

            db.Buildings.AddRange(buildings);
        }

        if (!await db.SupportTeams.AnyAsync())
        {
            var it = new SupportTeam { Name = "IT Support", Description = "Projectors, computers and lab equipment" };
            var network = new SupportTeam { Name = "Network", Description = "Wi-Fi and wired network" };
            var electrical = new SupportTeam { Name = "Electrical", Description = "Lights, fans, power sockets, AC" };
            var facilities = new SupportTeam { Name = "Furniture & Facilities", Description = "Chairs, desks, doors, general repairs" };
            db.SupportTeams.AddRange(it, network, electrical, facilities);

            // Categories point to their default team, so seed them together with the teams.
            if (!await db.IssueCategories.AnyAsync())
            {
                db.IssueCategories.AddRange(
                    new IssueCategory { Name = "Projector", DefaultSupportTeam = it },
                    new IssueCategory { Name = "Computer Lab", DefaultSupportTeam = it },
                    new IssueCategory { Name = "Wi-Fi / Network", DefaultSupportTeam = network },
                    new IssueCategory { Name = "Electrical", DefaultSupportTeam = electrical },
                    new IssueCategory { Name = "Furniture", DefaultSupportTeam = facilities },
                    new IssueCategory { Name = "Other", Description = "Anything not listed above" });
            }
        }

        if (!await db.AssetTypes.AnyAsync())
        {
            db.AssetTypes.AddRange(
                new AssetType { Name = "Projector" },
                new AssetType { Name = "Desktop PC" },
                new AssetType { Name = "Wi-Fi Access Point" },
                new AssetType { Name = "Air Conditioner" },
                new AssetType { Name = "Smart Board" },
                new AssetType { Name = "Furniture" });
        }

        await db.SaveChangesAsync();
    }
}
