using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

/// <summary>Flor ou categoria abrangida. Sem alvos, o cupão aplica-se a todos os artigos.</summary>
public class CouponTarget : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public Guid? FlowerId { get; set; }
    public Flower? Flower { get; set; }
    public string? Category { get; set; }
}
