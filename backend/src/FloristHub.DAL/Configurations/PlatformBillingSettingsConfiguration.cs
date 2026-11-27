using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class PlatformBillingSettingsConfiguration : IEntityTypeConfiguration<PlatformBillingSettings>
{
    public void Configure(EntityTypeBuilder<PlatformBillingSettings> builder)
    {
        builder.Property(x => x.CommissionPercent).HasPrecision(5, 2);
        builder.Property(x => x.MonthlyFeeAmount).HasPrecision(10, 2);
    }
}
