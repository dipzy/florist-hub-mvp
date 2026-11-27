using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class FloristInviteConfiguration : IEntityTypeConfiguration<FloristInvite>
{
    public void Configure(EntityTypeBuilder<FloristInvite> builder)
    {
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.ExpiresAt).IsRequired();
    }
}
