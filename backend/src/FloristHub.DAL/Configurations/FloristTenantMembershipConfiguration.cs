using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class FloristTenantMembershipConfiguration : IEntityTypeConfiguration<FloristTenantMembership>
{
    public void Configure(EntityTypeBuilder<FloristTenantMembership> builder)
    {
        builder.HasIndex(x => new { x.UserId, x.TenantId }).IsUnique();
        builder.HasIndex(x => x.TenantId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
