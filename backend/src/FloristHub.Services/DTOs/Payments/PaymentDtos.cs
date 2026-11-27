using FloristHub.DAL.Enums;

namespace FloristHub.Services.DTOs.Payments;

public record CheckoutSessionDto(
    bool PaymentsEnabled,
    string? CheckoutUrl,
    Guid? PaymentId,
    PaymentStatus? Status,
    string? Message);

public record StartCheckoutRequest(
    string? SuccessUrl,
    string? CancelUrl,
    string? CustomerTaxId = null,
    bool AcceptTermsAndPrivacy = false,
    string? TermsVersion = null,
    string? PrivacyVersion = null);

public record OrderPaymentDto(
    Guid Id,
    PaymentStatus Status,
    decimal Amount,
    decimal RefundedAmount,
    string Currency);

public record PaymentEventDto(
    Guid Id,
    string EventType,
    PaymentStatus? FromStatus,
    PaymentStatus ToStatus,
    decimal? Amount,
    string? Notes,
    DateTimeOffset CreatedAt);

public record PaymentDto(
    Guid Id,
    Guid OrderId,
    PaymentStatus Status,
    decimal Amount,
    decimal RefundedAmount,
    decimal CommissionPercent,
    long ApplicationFeeAmountCents,
    string Currency,
    string? FailureMessage,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PaymentEventDto> History);

public record RefundPaymentRequest(decimal? Amount, string? Reason);

public record StripeConnectStatusDto(
    bool PaymentsEnabled,
    bool Connected,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted,
    bool CardPayments,
    bool MbWay,
    bool Multibanco,
    string? AccountId,
    string? PublishableKey);

public record StripeOnboardingRequest(string ReturnUrl, string RefreshUrl);

public record StripeOnboardingResponse(bool PaymentsEnabled, string? Url, string? Message);

public record PlatformBillingDto(decimal CommissionPercent, decimal MonthlyFeeAmount);

public record UpdatePlatformBillingRequest(decimal CommissionPercent, decimal MonthlyFeeAmount);

public record TenantBillingDto(
    decimal? CommissionPercentOverride,
    decimal? MonthlyFeeAmount,
    decimal EffectiveCommissionPercent,
    decimal EffectiveMonthlyFeeAmount);

public record UpdateTenantBillingRequest(decimal? CommissionPercentOverride, decimal? MonthlyFeeAmount);
