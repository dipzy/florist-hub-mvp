using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class PortfolioArrangementConfiguration : IEntityTypeConfiguration<PortfolioArrangement>
{
    public void Configure(EntityTypeBuilder<PortfolioArrangement> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Slug).HasMaxLength(80);
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
    }
}
