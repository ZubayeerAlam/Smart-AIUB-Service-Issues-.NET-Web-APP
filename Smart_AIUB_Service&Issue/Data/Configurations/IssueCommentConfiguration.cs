using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Data.Configurations;

public class IssueCommentConfiguration : IEntityTypeConfiguration<IssueComment>
{
    public void Configure(EntityTypeBuilder<IssueComment> builder)
    {
        builder.Property(c => c.Body).IsRequired().HasMaxLength(2000);

        builder.HasOne(c => c.Issue)
            .WithMany(i => i.Comments)
            .HasForeignKey(c => c.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.IssueId, c.CreatedAt });
    }
}
