using System.Text.Json;
using FloristHub.Services.DTOs.Orders;
using FloristHub.Services.Invoicing;
using FloristHub.DAL.Enums;
using FloristHub.Services.Localization;
using FluentValidation;

namespace FloristHub.Services.Validation;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public const int MaxNotesLength = 2000;
    public const int MaxAddressLength = 500;
    public const int MaxItemNameLength = 200;

    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage(_ => ValidationMessages.OrderNeedsItems);
        RuleForEach(x => x.Items).SetValidator(new OrderItemRequestValidator());

        RuleFor(x => x.DeliveryAddress)
            .NotEmpty()
            .When(x => x.DeliveryType is DeliveryType.HomeDelivery or DeliveryType.CemeteryOrOther)
            .WithMessage(_ => ValidationMessages.DeliveryAddressRequired);
        RuleFor(x => x.DeliveryAddress)
            .MaximumLength(MaxAddressLength)
            .When(x => !string.IsNullOrWhiteSpace(x.DeliveryAddress));

        RuleFor(x => x.CustomerNotes).MaximumLength(MaxNotesLength);
        RuleFor(x => x.CustomerTaxId)
            .Must(value => string.IsNullOrWhiteSpace(value) || PortugueseNif.IsValid(value))
            .WithMessage(_ => ValidationMessages.InvalidNif);
        RuleFor(x => x.BaseType).IsInEnum();
        RuleFor(x => x.ArrangementShape).IsInEnum();
        RuleFor(x => x)
            .Must(x => x.ArrangementShape != ArrangementShape.Sponge || x.BaseType != ArrangementBaseType.Jar)
            .WithMessage(_ => ValidationMessages.SpongeShapeCannotUseJar);
        RuleFor(x => x.ArrangementPreviewUrl)
            .MaximumLength(2048)
            .Must(url =>
                string.IsNullOrWhiteSpace(url)
                || url.StartsWith('/')
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            .WithMessage(_ => ValidationMessages.InvalidArrangementPreviewUrl);
        RuleFor(x => x.DeliveryType).IsInEnum();

        RuleFor(x => x.RequestedDeliveryAt)
            .Must(d => !d.HasValue || d.Value > DateTimeOffset.UtcNow.AddMinutes(-5))
            .WithMessage(_ => ValidationMessages.DeliveryTimeMustBeFuture);
    }
}

public sealed class OrderItemRequestValidator : AbstractValidator<OrderItemRequest>
{
    public OrderItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(CreateOrderRequestValidator.MaxItemNameLength);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(999);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateOrderChangeProposalRequestValidator : AbstractValidator<CreateOrderChangeProposalRequest>
{
    public const int MaxMessageLength = 2000;

    public CreateOrderChangeProposalRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(MaxMessageLength);
        RuleFor(x => x.RevisedTotal).GreaterThanOrEqualTo(0).When(x => x.RevisedTotal.HasValue);
        RuleFor(x => x.ProposedItemChangesJson)
            .Must(OrderItemChangesJsonValidator.IsValidOrEmpty)
            .WithMessage(_ => ValidationMessages.InvalidProposedItemsJson);
    }
}

public sealed class FloristOrderActionRequestValidator : AbstractValidator<FloristOrderActionRequest>
{
    public FloristOrderActionRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

/// <summary>
/// Valida e deserializa o JSON de itens nas propostas de alteração.
/// </summary>
public static class OrderItemChangesJsonValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool IsValidOrEmpty(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return true;
        return TryParse(json, out _);
    }

    public static bool TryParse(string json, out List<OrderItemRequest> items)
    {
        items = [];
        try
        {
            var parsed = JsonSerializer.Deserialize<List<OrderItemRequest>>(json, JsonOptions);
            if (parsed is null || parsed.Count == 0)
                return false;

            var itemValidator = new OrderItemRequestValidator();
            foreach (var item in parsed)
            {
                var result = itemValidator.Validate(item);
                if (!result.IsValid)
                    return false;
            }

            items = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public static class OrderValidation
{
    private static readonly CreateOrderRequestValidator CreateValidator = new();
    private static readonly CreateOrderChangeProposalRequestValidator ProposalValidator = new();
    private static readonly FloristOrderActionRequestValidator FloristActionValidator = new();

    public static void EnsureValid(CreateOrderRequest request)
    {
        var result = CreateValidator.Validate(request);
        if (!result.IsValid)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
    }

    public static void EnsureValid(CreateOrderChangeProposalRequest request)
    {
        var result = ProposalValidator.Validate(request);
        if (!result.IsValid)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
    }

    public static void EnsureValid(FloristOrderActionRequest request)
    {
        var result = FloristActionValidator.Validate(request);
        if (!result.IsValid)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
    }

    public static void EnsureRejectReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException(ValidationMessages.RejectReasonRequired);
        if (reason.Length > 2000)
            throw new InvalidOperationException(ValidationMessages.RejectReasonTooLong);
    }
}
