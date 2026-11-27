using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class DeliveryCapacityPolicyConfiguration : IEntityTypeConfiguration<DeliveryCapacityPolicy>
{
    public void Configure(EntityTypeBuilder<DeliveryCapacityPolicy> builder)
    {
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.Property(x => x.DailyOrderLimit);
    }
}

public class DeliveryCapacityWindowConfiguration : IEntityTypeConfiguration<DeliveryCapacityWindow>
{
    public void Configure(EntityTypeBuilder<DeliveryCapacityWindow> builder)
    {
        builder.Property(x => x.Label).HasMaxLength(40);
        builder.HasIndex(x => new { x.TenantId, x.SortOrder });
    }
}

public class DeliveryCapacityExceptionConfiguration : IEntityTypeConfiguration<DeliveryCapacityException>
{
    public void Configure(EntityTypeBuilder<DeliveryCapacityException> builder)
    {
        builder.Property(x => x.Note).HasMaxLength(400);
        builder.HasIndex(x => new { x.TenantId, x.ServiceDate }).IsUnique();
    }
}

public class DeliveryCapacityHoldConfiguration : IEntityTypeConfiguration<DeliveryCapacityHold>
{
    public void Configure(EntityTypeBuilder<DeliveryCapacityHold> builder)
    {
        builder.Property(x => x.ScopeKey).HasMaxLength(32);
        builder.HasIndex(x => new { x.TenantId, x.ServiceDate, x.ScopeKey }).IsUnique();
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.Property(x => x.Note).HasMaxLength(400);
        builder.HasIndex(x => new { x.TenantId, x.FlowerId, x.CreatedAt });
        builder.HasOne(x => x.Flower).WithMany().HasForeignKey(x => x.FlowerId).OnDelete(DeleteBehavior.Cascade);
    }
}
