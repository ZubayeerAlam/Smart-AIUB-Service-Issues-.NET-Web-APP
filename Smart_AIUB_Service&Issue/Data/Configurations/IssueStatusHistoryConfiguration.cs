using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class IssueStatusHistoryConfiguration : IEntityTypeConfiguration<IssueStatusHistory>
{
    public void Configure(EntityTypeBuilder<IssueStatusHistory> builder)
    {
        builder.ToTable("IssueStatusHistory");

        builder.Property(h => h.Note).HasMaxLength(1000);

        builder.HasOne(h => h.Issue)
            .WithMany(i => i.StatusHistory)
            .HasForeignKey(h => h.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Timeline on the issue detail page is read in date order.
        builder.HasIndex(h => new { h.IssueId, h.ChangedAt });
    }
}
