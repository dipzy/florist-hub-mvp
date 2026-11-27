using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasOne(x => x.Tenant).WithMany(t => t.Users).HasForeignKey(x => x.TenantId).IsRequired(false);
        builder.Property(x => x.AcceptedTermsVersion).HasMaxLength(32);
        builder.Property(x => x.AcceptedPrivacyVersion).HasMaxLength(32);
    }
}
