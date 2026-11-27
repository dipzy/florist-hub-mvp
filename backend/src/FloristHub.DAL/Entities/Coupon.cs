using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

public class Coupon : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public required string Code { get; set; }
    public CouponDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }
    public int? MaxRedemptions { get; set; }
    public int? MaxRedemptionsPerCustomer { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CouponTarget> Targets { get; set; } = new List<CouponTarget>();
    public ICollection<CouponRedemption> Redemptions { get; set; } = new List<CouponRedemption>();
}
