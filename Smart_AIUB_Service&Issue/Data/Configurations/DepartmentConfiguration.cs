using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.Property(d => d.Code).IsRequired().HasMaxLength(20);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(d => d.Code).IsUnique();
        builder.HasIndex(d => d.Name).IsUnique();
    }
}
