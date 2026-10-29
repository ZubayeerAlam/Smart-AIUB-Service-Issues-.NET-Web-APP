using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.Property(b => b.Code).IsRequired().HasMaxLength(20);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(b => b.Code).IsUnique();
        builder.HasIndex(b => b.Name).IsUnique();
    }
}
