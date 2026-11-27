using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.EstimatedTotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.CouponCode).HasMaxLength(32);
        builder.HasOne(x => x.Coupon).WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.SetNull);
        builder.Property(x => x.ArrangementPreviewUrl).HasMaxLength(2048);
        builder.Property(x => x.AcceptedTermsVersion).HasMaxLength(32);
        builder.Property(x => x.AcceptedPrivacyVersion).HasMaxLength(32);
        builder.Property(x => x.DeliveryFee).HasPrecision(10, 2);
        builder.Property(x => x.CustomerTaxId).HasMaxLength(16);
        builder.Property(x => x.CapacityWindowKey).HasMaxLength(32);
        builder.Property(x => x.TrackingAccessToken).HasMaxLength(64);
        builder.HasIndex(x => x.TrackingAccessToken).IsUnique().HasFilter("\"TrackingAccessToken\" IS NOT NULL");
        builder.Property(x => x.ClientSubmissionId).HasMaxLength(128);
        builder.HasIndex(x => new { x.TenantId, x.ClientSubmissionId })
            .IsUnique()
            .HasFilter("\"ClientSubmissionId\" IS NOT NULL");
        builder.HasOne(x => x.DeliveryZone).WithMany().HasForeignKey(x => x.DeliveryZoneId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.RecurringSubscription).WithMany().HasForeignKey(x => x.RecurringSubscriptionId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.RecurringOccurrence).WithMany().HasForeignKey(x => x.RecurringOccurrenceId).OnDelete(DeleteBehavior.SetNull);
    }
}
