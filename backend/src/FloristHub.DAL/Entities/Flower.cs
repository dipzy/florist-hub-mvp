using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class Flower : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public string? Color { get; set; }
    public decimal UnitPrice { get; set; }
    /// <summary>Taxa de IVA em percentagem. Nulo usa a taxa por defeito da loja. O preço unitário inclui IVA.</summary>
    public decimal? VatRate { get; set; }
    public string Unit { get; set; } = "unidade";
    public int? StockQuantity { get; set; }
    public int? MinimumStockQuantity { get; set; }
    public bool IsApproximatelyAvailable { get; set; } = true;
    public bool IsCatalogActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }

    /// <summary>Categoria livre da loja (ex. rosas, plantas). Usada pelos cupões.</summary>
    public string? Category { get; set; }

    /// <summary>Meses em que a flor é da época. Bit 0 = janeiro.</summary>
    public int SeasonMonths { get; set; }
}
