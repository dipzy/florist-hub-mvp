using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class CouponRedemption : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid CustomerId { get; set; }
    public decimal DiscountAmount { get; set; }
    public bool IsVoided { get; set; }
}
