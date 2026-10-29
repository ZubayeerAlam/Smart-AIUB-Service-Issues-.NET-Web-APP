using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.Property(i => i.Title).IsRequired().HasMaxLength(100);
        builder.Property(i => i.Description).IsRequired().HasMaxLength(2000);
        builder.Property(i => i.ResolutionNote).HasMaxLength(2000);

        builder.Property(i => i.RowVersion).IsRowVersion();

        // Restrict everywhere: issues are never deleted, and lookups/users are
        // deactivated instead of deleted, so a delete here would be a bug.
        builder.HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Room)
            .WithMany()
            .HasForeignKey(i => i.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Reporter)
            .WithMany()
            .HasForeignKey(i => i.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.AssignedTeam)
            .WithMany()
            .HasForeignKey(i => i.AssignedTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.AssignedStaff)
            .WithMany()
            .HasForeignKey(i => i.AssignedStaffId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Asset)
            .WithMany()
            .HasForeignKey(i => i.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        // Extra indexes for the queue/dashboard/report filters.
        // (EF Core already indexes every foreign key column on its own.)
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.CreatedAt);
        builder.HasIndex(i => new { i.AssignedTeamId, i.Status });
        builder.HasIndex(i => new { i.AssignedStaffId, i.Status });
    }
}
