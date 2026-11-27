using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class TenantOpeningHourConfiguration : IEntityTypeConfiguration<TenantOpeningHour>
{
    public void Configure(EntityTypeBuilder<TenantOpeningHour> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.DayOfWeek }).IsUnique();
        builder.Property(x => x.OpenTime).HasConversion(
            v => v.HasValue ? TimeSpan.FromTicks(v.Value.Ticks) : (TimeSpan?)null,
            v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null);
        builder.Property(x => x.CloseTime).HasConversion(
            v => v.HasValue ? TimeSpan.FromTicks(v.Value.Ticks) : (TimeSpan?)null,
            v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null);
    }
}
