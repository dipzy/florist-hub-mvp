using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class LegalDocumentVersionConfiguration : IEntityTypeConfiguration<LegalDocumentVersion>
{
    public void Configure(EntityTypeBuilder<LegalDocumentVersion> builder)
    {
        builder.HasIndex(x => new { x.DocumentType, x.Version }).IsUnique();
        builder.HasIndex(x => new { x.DocumentType, x.IsCurrent });
        builder.Property(x => x.Version).HasMaxLength(32);
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.ContentMarkdown).HasMaxLength(64_000);
    }
}
