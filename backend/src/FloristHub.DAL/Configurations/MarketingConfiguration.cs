using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(32);
        builder.Property(x => x.DiscountValue).HasPrecision(18, 2);
        builder.Property(x => x.MinimumOrderAmount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

public class CouponTargetConfiguration : IEntityTypeConfiguration<CouponTarget>
{
    public void Configure(EntityTypeBuilder<CouponTarget> builder)
    {
        builder.Property(x => x.Category).HasMaxLength(80);
        builder.HasIndex(x => new { x.CouponId, x.FlowerId });
        builder.HasIndex(x => new { x.CouponId, x.Category });
        builder.HasOne(x => x.Coupon).WithMany(x => x.Targets).HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Flower).WithMany().HasForeignKey(x => x.FlowerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.OrderId, x.IsVoided });
        builder.HasIndex(x => new { x.CouponId, x.CustomerId });
        builder.HasOne(x => x.Coupon).WithMany(x => x.Redemptions).HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SeasonalCampaignConfiguration : IEntityTypeConfiguration<SeasonalCampaign>
{
    public void Configure(EntityTypeBuilder<SeasonalCampaign> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(120);
        builder.Property(x => x.Slug).HasMaxLength(80);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
    }
}

public class CampaignFlowerConfiguration : IEntityTypeConfiguration<CampaignFlower>
{
    public void Configure(EntityTypeBuilder<CampaignFlower> builder)
    {
        builder.Property(x => x.SpecialPrice).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CampaignId, x.FlowerId }).IsUnique();
        builder.HasOne(x => x.Campaign).WithMany(x => x.Flowers).HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Flower).WithMany().HasForeignKey(x => x.FlowerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderReviewConfiguration : IEntityTypeConfiguration<OrderReview>
{
    public void Configure(EntityTypeBuilder<OrderReview> builder)
    {
        builder.Property(x => x.Comment).HasMaxLength(2000);
        builder.Property(x => x.FloristReply).HasMaxLength(2000);
        builder.Property(x => x.HiddenReason).HasMaxLength(500);
        builder.Property(x => x.IsApproved).HasDefaultValue(false);
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasOne(x => x.Order).WithOne(x => x.Review).HasForeignKey<OrderReview>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
