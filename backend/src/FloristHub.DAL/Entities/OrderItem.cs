using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class OrderItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid? FlowerId { get; set; }
    public Flower? Flower { get; set; }
    public required string Name { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    /// <summary>Categoria da flor no momento da encomenda, para o cupão.</summary>
    public string? Category { get; set; }
    /// <summary>Taxa de IVA em percentagem, copiada do catálogo no momento da encomenda.</summary>
    public decimal VatRate { get; set; } = 23m;
}
