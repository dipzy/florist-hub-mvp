using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class DeliveryZoneConfiguration : IEntityTypeConfiguration<DeliveryZone>
{
    public void Configure(EntityTypeBuilder<DeliveryZone> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(120);
        builder.Property(x => x.Fee).HasPrecision(10, 2);
        builder.HasIndex(x => new { x.TenantId, x.Name });
    }
}
