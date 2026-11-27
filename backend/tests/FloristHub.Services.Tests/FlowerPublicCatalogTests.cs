namespace FloristHub.Services.Tests;

public class FlowerPublicCatalogTests
{
    [Fact]
    public void In_stock_flower_is_for_sale_even_when_legacy_flag_is_false()
    {
        Assert.True(FlowerPublicCatalog.IsForSale(true, 69, false));
        Assert.True(FlowerPublicCatalog.PublicApproximatelyAvailable(true, 69, false, false));
    }

    [Fact]
    public void Out_of_stock_stays_unavailable()
    {
        Assert.False(FlowerPublicCatalog.IsForSale(true, 0, true));
        Assert.False(FlowerPublicCatalog.PublicApproximatelyAvailable(true, 0, true, false));
    }
}
