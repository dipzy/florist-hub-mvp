using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class RecurringSubscriptionItemConfiguration : IEntityTypeConfiguration<RecurringSubscriptionItem>
{
    public void Configure(EntityTypeBuilder<RecurringSubscriptionItem> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(256);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.HasOne(x => x.RecurringSubscription)
            .WithMany(s => s.Items)
            .HasForeignKey(x => x.RecurringSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
