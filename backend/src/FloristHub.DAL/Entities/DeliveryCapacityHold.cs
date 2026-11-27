using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

/// <summary>Contador de vagas ocupadas. <c>ScopeKey</c> é <c>day</c> ou a janela <c>HH:mm-HH:mm</c>.</summary>
public class DeliveryCapacityHold : BaseEntity, ITenantEntity
{
    public const string DayScope = "day";

    public Guid TenantId { get; set; }
    public DateOnly ServiceDate { get; set; }
    public required string ScopeKey { get; set; }
    public int BookedCount { get; set; }
}
