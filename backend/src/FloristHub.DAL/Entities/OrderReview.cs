using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class OrderReview : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid CustomerId { get; set; }
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public string? FloristReply { get; set; }
    public DateTimeOffset? FloristRepliedAt { get; set; }
    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }
    public bool IsApproved { get; set; }
}
