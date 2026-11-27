using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.CustomDomain).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.SeoTitle).HasMaxLength(70);
        builder.Property(x => x.SeoDescription).HasMaxLength(300);
        builder.Property(x => x.Slug).HasMaxLength(100);
        builder.Property(x => x.CustomDomain).HasMaxLength(253);
        builder.Property(x => x.ThemePrimaryColor).HasMaxLength(9);
        builder.Property(x => x.ThemeAccentColor).HasMaxLength(9);
        builder.Property(x => x.ThemeFontFamily).HasMaxLength(32);
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.Property(x => x.AddressLine).HasMaxLength(300);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.StripeAccountId).HasMaxLength(255);
        builder.HasIndex(x => x.StripeAccountId).IsUnique();
        builder.Property(x => x.CommissionPercentOverride).HasPrecision(5, 2);
        builder.Property(x => x.MonthlyFeeAmount).HasPrecision(10, 2);
        builder.HasMany(x => x.OpeningHours).WithOne().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.DeliveryZones).WithOne().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
