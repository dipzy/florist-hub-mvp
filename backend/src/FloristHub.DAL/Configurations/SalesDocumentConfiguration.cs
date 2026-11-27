using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class SalesDocumentConfiguration : IEntityTypeConfiguration<SalesDocument>
{
    public void Configure(EntityTypeBuilder<SalesDocument> builder)
    {
        builder.Property(x => x.IdempotencyKey).HasMaxLength(120);
        builder.Property(x => x.ProviderDocumentId).HasMaxLength(64);
        builder.Property(x => x.DocumentNumber).HasMaxLength(80);
        builder.Property(x => x.Permalink).HasMaxLength(500);
        builder.Property(x => x.PdfStoragePath).HasMaxLength(300);
        builder.Property(x => x.CustomerTaxId).HasMaxLength(16);
        builder.Property(x => x.LastError).HasMaxLength(1000);
        builder.Property(x => x.GrossAmount).HasPrecision(12, 2);
        builder.Property(x => x.NetAmount).HasPrecision(12, 2);
        builder.Property(x => x.VatAmount).HasPrecision(12, 2);

        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
        builder.HasIndex(x => new { x.TenantId, x.OrderId });

        builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
    }
}
