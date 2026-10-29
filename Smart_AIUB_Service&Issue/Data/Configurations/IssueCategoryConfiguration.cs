using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class IssueCategoryConfiguration : IEntityTypeConfiguration<IssueCategory>
{
    public void Configure(EntityTypeBuilder<IssueCategory> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasOne(c => c.DefaultSupportTeam)
            .WithMany()
            .HasForeignKey(c => c.DefaultSupportTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
