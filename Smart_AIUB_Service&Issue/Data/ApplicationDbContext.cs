using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;

namespace Smart_AIUB_Service_Issue.Data;

/// <summary>
/// The EF Core "session" with the database. Inheriting from IdentityDbContext
/// adds the ASP.NET Core Identity tables (AspNetUsers, AspNetRoles, ...).
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<SupportTeam> SupportTeams => Set<SupportTeam>();
    public DbSet<IssueCategory> IssueCategories => Set<IssueCategory>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueStatusHistory> IssueStatusHistory => Set<IssueStatusHistory>();
    public DbSet<IssueComment> IssueComments => Set<IssueComment>();
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must run first: it configures the Identity tables.
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> class in Data/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Store enums as readable text ("InProgress") instead of numbers (2),
        // so the data makes sense when browsing tables in SSMS.
        configurationBuilder.Properties<IssueStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<IssuePriority>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<AssetStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<MaintenanceType>().HaveConversion<string>().HaveMaxLength(20);
    }
}
