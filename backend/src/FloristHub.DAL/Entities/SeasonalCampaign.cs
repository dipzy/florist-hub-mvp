using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class SeasonalCampaign : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public bool HighlightOnHome { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CampaignFlower> Flowers { get; set; } = new List<CampaignFlower>();
}
