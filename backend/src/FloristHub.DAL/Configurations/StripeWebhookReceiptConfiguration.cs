using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class StripeWebhookReceiptConfiguration : IEntityTypeConfiguration<StripeWebhookReceipt>
{
    public void Configure(EntityTypeBuilder<StripeWebhookReceipt> builder)
    {
        builder.Property(x => x.StripeEventId).HasMaxLength(255);
        builder.Property(x => x.EventType).HasMaxLength(80);
        builder.HasIndex(x => x.StripeEventId).IsUnique();
    }
}
