using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

public class PortfolioArrangement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public required string Title { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Tags { get; set; }
    public PortfolioArrangementStatus Status { get; set; } = PortfolioArrangementStatus.Active;
    public int SortOrder { get; set; }

    public ICollection<PortfolioArrangementPhoto> Photos { get; set; } = new List<PortfolioArrangementPhoto>();
    public ICollection<PortfolioArrangementItem> Items { get; set; } = new List<PortfolioArrangementItem>();
}
