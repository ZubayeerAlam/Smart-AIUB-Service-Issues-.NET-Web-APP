using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.Property(a => a.AssetTag).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(150);
        builder.Property(a => a.SerialNumber).HasMaxLength(100);
        builder.Property(a => a.Notes).HasMaxLength(1000);

        builder.HasIndex(a => a.AssetTag).IsUnique();

        builder.HasOne(a => a.AssetType)
            .WithMany()
            .HasForeignKey(a => a.AssetTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Room)
            .WithMany()
            .HasForeignKey(a => a.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
