using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.Property(r => r.RoomNumber).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Name).HasMaxLength(100);

        // The same room number can exist in different buildings, but not twice in one building.
        builder.HasIndex(r => new { r.BuildingId, r.RoomNumber }).IsUnique();

        builder.HasOne(r => r.Building)
            .WithMany(b => b.Rooms)
            .HasForeignKey(r => r.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
