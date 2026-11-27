using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> builder)
    {
        builder.Property(x => x.EventType).HasMaxLength(80);
        builder.Property(x => x.StripeEventId).HasMaxLength(255);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.HasIndex(x => x.StripeEventId);

        builder.HasOne(x => x.Payment)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
