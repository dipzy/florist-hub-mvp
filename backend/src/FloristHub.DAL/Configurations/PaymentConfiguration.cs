using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.RefundedAmount).HasPrecision(12, 2);
        builder.Property(x => x.CommissionPercent).HasPrecision(5, 2);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.StripeCheckoutSessionId).HasMaxLength(255);
        builder.Property(x => x.StripePaymentIntentId).HasMaxLength(255);
        builder.Property(x => x.StripeChargeId).HasMaxLength(255);
        builder.Property(x => x.StripeConnectedAccountId).HasMaxLength(255);
        builder.Property(x => x.CheckoutUrl).HasMaxLength(2000);
        builder.Property(x => x.FailureMessage).HasMaxLength(1000);

        builder.HasIndex(x => x.StripeCheckoutSessionId);
        builder.HasIndex(x => x.StripePaymentIntentId);
        builder.HasIndex(x => new { x.TenantId, x.OrderId });

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
