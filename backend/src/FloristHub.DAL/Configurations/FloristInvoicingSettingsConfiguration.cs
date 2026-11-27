using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class FloristInvoicingSettingsConfiguration : IEntityTypeConfiguration<FloristInvoicingSettings>
{
    public void Configure(EntityTypeBuilder<FloristInvoicingSettings> builder)
    {
        builder.Property(x => x.LegalName).HasMaxLength(200);
        builder.Property(x => x.TaxId).HasMaxLength(16);
        builder.Property(x => x.AddressLine).HasMaxLength(300);
        builder.Property(x => x.City).HasMaxLength(120);
        builder.Property(x => x.PostalCode).HasMaxLength(16);
        builder.Property(x => x.Country).HasMaxLength(80);
        builder.Property(x => x.AccountName).HasMaxLength(64);
        builder.Property(x => x.ProtectedApiKey).HasMaxLength(2000);
        builder.Property(x => x.DocumentSeries).HasMaxLength(40);
        builder.Property(x => x.DefaultVatRate).HasPrecision(5, 2);
        builder.Property(x => x.TaxExemptionCode).HasMaxLength(8);
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
