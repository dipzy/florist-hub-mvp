using FloristHub.Services.DTOs.Orders;
using FloristHub.Services.DTOs.Recurring;

namespace FloristHub.Services.Recurring;

internal static class RecurringSubscriptionValidation
{
    public static void EnsureValid(CreateRecurringSubscriptionRequest request)
    {
        if (request.AnchorDeliveryAt == default)
            throw new InvalidOperationException("Indique a primeira entrega.");

        if (request.IsFloristChoice)
        {
            if (request.FloristChoiceBudget is not > 0)
                throw new InvalidOperationException("Indique o orçamento máximo para a escolha da florista.");
            return;
        }

        if (request.Items.Count == 0)
            throw new InvalidOperationException("Adicione pelo menos uma flor ou ative a escolha da florista.");

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new InvalidOperationException("Cada linha precisa de um nome.");
            if (item.Quantity <= 0)
                throw new InvalidOperationException("Quantidade inválida.");
            if (item.UnitPrice < 0)
                throw new InvalidOperationException("Preço inválido.");
        }
    }

    public static CreateOrderRequest ToOrderRequest(
        CreateRecurringSubscriptionRequest request,
        DateTimeOffset? deliveryAt = null) =>
        new(
            request.BaseType,
            request.DeliveryType,
            request.DeliveryAddress,
            deliveryAt ?? request.AnchorDeliveryAt,
            request.CustomerNotes,
            null,
            request.DeliveryZoneId,
            request.IsFloristChoice
                ? [new OrderItemRequest(null, "Escolha da florista", 1, request.FloristChoiceBudget ?? 0)]
                : request.Items,
            request.ArrangementShape,
            request.ArrangementVariationSeed,
            request.ArrangementPreviewUrl,
            request.AcceptTermsAndPrivacy,
            request.TermsVersion,
            request.PrivacyVersion);
}
