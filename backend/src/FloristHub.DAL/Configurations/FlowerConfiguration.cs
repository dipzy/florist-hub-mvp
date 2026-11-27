using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class FlowerConfiguration : IEntityTypeConfiguration<Flower>
{
    public void Configure(EntityTypeBuilder<Flower> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Unit).HasMaxLength(40).HasDefaultValue("unidade");
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.Category).HasMaxLength(80);
        builder.Property(x => x.VatRate).HasPrecision(5, 2);
    }
}
