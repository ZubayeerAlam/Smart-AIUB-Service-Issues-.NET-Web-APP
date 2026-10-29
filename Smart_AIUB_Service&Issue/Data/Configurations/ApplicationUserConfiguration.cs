using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.UniversityId).HasMaxLength(20);

        // Unique when present. SQL Server gets a filtered index (WHERE UniversityId IS NOT NULL)
        // so many users without an ID are still allowed.
        builder.HasIndex(u => u.UniversityId).IsUnique();

        builder.HasOne(u => u.Department)
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.SupportTeam)
            .WithMany(t => t.Members)
            .HasForeignKey(u => u.SupportTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
