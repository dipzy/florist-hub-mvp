using FloristHub.DAL.Enums;
using FloristHub.Services.DTOs.Orders;
using FloristHub.Services.Localization;
using FloristHub.Services.Validation;

namespace FloristHub.Services.Tests.Localization;

public class ValidationMessagesCultureTests
{
    private static CreateOrderRequest EmptyItemsRequest() =>
        new(
            ArrangementBaseType.Jar,
            DeliveryType.HomeDelivery,
            null,
            null,
            null,
            null,
            null,
            Array.Empty<OrderItemRequest>(),
            ArrangementShape.Sponge);

    [Fact]
    public void FluentValidation_uses_english_when_culture_is_en()
    {
        RequestCultureScope.SetCulture("en");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => OrderValidation.EnsureValid(EmptyItemsRequest()));
            Assert.Contains("item", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            RequestCultureScope.SetCulture(null);
        }
    }

    [Fact]
    public void FluentValidation_defaults_to_portuguese()
    {
        RequestCultureScope.SetCulture(null);
        var ex = Assert.Throws<InvalidOperationException>(() => OrderValidation.EnsureValid(EmptyItemsRequest()));
        Assert.Contains("encomenda", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
