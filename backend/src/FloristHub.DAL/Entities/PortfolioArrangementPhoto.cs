using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class PortfolioArrangementPhoto : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PortfolioArrangementId { get; set; }
    public PortfolioArrangement? PortfolioArrangement { get; set; }
    public required string Url { get; set; }
    public int SortOrder { get; set; }
}
