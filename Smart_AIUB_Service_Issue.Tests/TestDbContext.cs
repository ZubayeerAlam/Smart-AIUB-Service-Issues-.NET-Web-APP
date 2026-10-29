using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Tests;

/// <summary>
/// The real context plus one SQLite fix: SQL Server fills Issue.RowVersion automatically,
/// SQLite has no such type, so here the column gets a random default value on insert.
/// </summary>
public class TestDbContext : ApplicationDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Issue>().Property(i => i.RowVersion).HasDefaultValueSql("randomblob(8)");
    }
}
