namespace FloristHub.Services;

public static class FlowerPublicCatalog
{
    /// <summary>
    /// Flor à venda no catálogo público: activa e com stock (nulo = sem contagem exacta).
    /// O booleano IsApproximatelyAvailable ficou a false em flores criadas pelo onboarding
    /// e não deve esconder artigos com stock.
    /// </summary>
    public static bool IsForSale(bool isCatalogActive, int? stockQuantity, bool isOutOfStock)
    {
        if (!isCatalogActive || isOutOfStock)
            return false;
        return stockQuantity is not <= 0;
    }

    public static bool PublicApproximatelyAvailable(bool isCatalogActive, int? stockQuantity, bool isOutOfStock, bool storedFlag) =>
        IsForSale(isCatalogActive, stockQuantity, isOutOfStock) || storedFlag;
}
