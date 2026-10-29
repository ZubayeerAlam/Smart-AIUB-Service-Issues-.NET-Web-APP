using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class AssetTypeConfiguration : IEntityTypeConfiguration<AssetType>
{
    public void Configure(EntityTypeBuilder<AssetType> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}
