using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class RecurringSubscriptionConfiguration : IEntityTypeConfiguration<RecurringSubscription>
{
    public void Configure(EntityTypeBuilder<RecurringSubscription> builder)
    {
        builder.Property(x => x.FloristChoiceBudget).HasPrecision(18, 2);
        builder.Property(x => x.DeliveryAddress).HasMaxLength(500);
        builder.Property(x => x.CustomerNotes).HasMaxLength(2000);
        builder.Property(x => x.FloristResponseNotes).HasMaxLength(2000);
        builder.Property(x => x.ArrangementPreviewUrl).HasMaxLength(2048);
        builder.HasOne(x => x.DeliveryZone).WithMany().HasForeignKey(x => x.DeliveryZoneId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => new { x.TenantId, x.Status, x.NextDeliveryAt });
    }
}
