using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.Property(m => m.Description).IsRequired().HasMaxLength(2000);

        // decimal(10,2): up to 99,999,999.99 BDT.
        builder.Property(m => m.Cost).HasPrecision(10, 2);

        builder.HasOne(m => m.Asset)
            .WithMany(a => a.MaintenanceRecords)
            .HasForeignKey(m => m.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Issue)
            .WithMany()
            .HasForeignKey(m => m.IssueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.PerformedBy)
            .WithMany()
            .HasForeignKey(m => m.PerformedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Asset history page lists records newest first.
        builder.HasIndex(m => new { m.AssetId, m.PerformedAt });
    }
}
