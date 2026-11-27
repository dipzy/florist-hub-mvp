using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class PortfolioArrangementItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PortfolioArrangementId { get; set; }
    public PortfolioArrangement? PortfolioArrangement { get; set; }
    public Guid? FlowerId { get; set; }
    public Flower? Flower { get; set; }
    public required string Name { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsMaterial { get; set; }
}
