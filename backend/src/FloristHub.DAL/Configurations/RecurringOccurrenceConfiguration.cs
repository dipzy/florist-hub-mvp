using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class RecurringOccurrenceConfiguration : IEntityTypeConfiguration<RecurringOccurrence>
{
    public void Configure(EntityTypeBuilder<RecurringOccurrence> builder)
    {
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasOne(x => x.RecurringSubscription)
            .WithMany(s => s.Occurrences)
            .HasForeignKey(x => x.RecurringSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.GeneratedOrder)
            .WithMany()
            .HasForeignKey(x => x.GeneratedOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
