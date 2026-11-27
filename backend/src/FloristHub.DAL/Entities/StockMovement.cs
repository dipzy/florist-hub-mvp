using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

public class StockMovement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid FlowerId { get; set; }
    public Flower? Flower { get; set; }
    public Guid? OrderId { get; set; }
    public int Delta { get; set; }
    public int? BalanceAfter { get; set; }
    public StockMovementReason Reason { get; set; }
    public string? Note { get; set; }
}
