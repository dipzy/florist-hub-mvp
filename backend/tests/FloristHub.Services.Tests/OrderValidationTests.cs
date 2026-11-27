using FloristHub.Services.DTOs.Orders;
using FloristHub.Services.Validation;
using FloristHub.DAL.Enums;

namespace FloristHub.Services.Tests;

public class OrderValidationTests
{
    [Fact]
    public void Create_order_requires_address_for_home_delivery()
    {
        var request = new CreateOrderRequest(
            ArrangementBaseType.Jar,
            DeliveryType.HomeDelivery,
            null,
            null,
            null,
            null,
            null,
            [new OrderItemRequest(null, "Rosa", 1, 2m)]);

        Assert.Throws<InvalidOperationException>(() => OrderValidation.EnsureValid(request));
    }

    [Fact]
    public void Create_order_accepts_arrangement_shape_seed_and_preview()
    {
        var request = new CreateOrderRequest(
            ArrangementBaseType.Wrapped,
            DeliveryType.StorePickup,
            null,
            null,
            null,
            null,
            null,
            [new OrderItemRequest(null, "Rosa", 3, 2m)],
            ArrangementShape.Long,
            90210,
            "/api/files/tenant/arranjo-3d.png");

        OrderValidation.EnsureValid(request);
        Assert.Equal(ArrangementShape.Long, request.ArrangementShape);
        Assert.Equal(90210, request.ArrangementVariationSeed);
        Assert.Equal(ArrangementBaseType.Wrapped, request.BaseType);
    }

    [Fact]
    public void Create_order_rejects_unknown_shape_and_preview_url()
    {
        var badShape = new CreateOrderRequest(
            ArrangementBaseType.Jar,
            DeliveryType.StorePickup,
            null,
            null,
            null,
            null,
            null,
            [new OrderItemRequest(null, "Rosa", 1, 2m)],
            (ArrangementShape)9);

        Assert.Throws<InvalidOperationException>(() => OrderValidation.EnsureValid(badShape));

        var badUrl = new CreateOrderRequest(
            ArrangementBaseType.Sponge,
            DeliveryType.StorePickup,
            null,
            null,
            null,
            null,
            null,
            [new OrderItemRequest(null, "Rosa", 1, 2m)],
            ArrangementShape.Short,
            4,
            "javascript:alert(1)");

        Assert.Throws<InvalidOperationException>(() => OrderValidation.EnsureValid(badUrl));
    }

    [Fact]
    public void Create_order_rejects_sponge_shape_with_jar_base()
    {
        var request = new CreateOrderRequest(
            ArrangementBaseType.Jar,
            DeliveryType.StorePickup,
            null,
            null,
            null,
            null,
            null,
            [new OrderItemRequest(null, "Rosa", 1, 2m)],
            ArrangementShape.Sponge);

        Assert.Throws<InvalidOperationException>(() => OrderValidation.EnsureValid(request));
    }

    [Fact]
    public void Create_order_accepts_sponge_shape_with_sponge_base()
    {
        var request = new CreateOrderRequest(
            ArrangementBaseType.Sponge,
            DeliveryType.StorePickup,
            null,
            null,
            null,
            null,
            null,
            [new OrderItemRequest(null, "Rosa", 1, 2m)],
            ArrangementShape.Sponge);

        OrderValidation.EnsureValid(request);
    }

    [Fact]
    public void Proposal_json_must_contain_valid_items()
    {
        var proposal = new CreateOrderChangeProposalRequest("Substituir flores", "{not-json", null);
        Assert.Throws<InvalidOperationException>(() => OrderValidation.EnsureValid(proposal));
    }

    [Fact]
    public void OrderItemChangesJsonValidator_parses_items()
    {
        const string json = """[{"name":"Rosa","quantity":2,"unitPrice":1.5}]""";
        Assert.True(OrderItemChangesJsonValidator.TryParse(json, out var items));
        Assert.Single(items);
        Assert.Equal("Rosa", items[0].Name);
    }
}
