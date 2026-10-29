using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class SupportTeamConfiguration : IEntityTypeConfiguration<SupportTeam>
{
    public void Configure(EntityTypeBuilder<SupportTeam> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Description).HasMaxLength(500);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}
