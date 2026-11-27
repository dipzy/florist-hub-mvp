using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class CampaignFlower : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CampaignId { get; set; }
    public SeasonalCampaign? Campaign { get; set; }
    public Guid FlowerId { get; set; }
    public Flower? Flower { get; set; }
    public decimal? SpecialPrice { get; set; }
    public int SortOrder { get; set; }
}
