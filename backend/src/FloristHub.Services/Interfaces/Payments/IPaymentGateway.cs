namespace FloristHub.Services.Interfaces.Payments;

/// <summary>
/// Ponto de extensão antigo. O Checkout Stripe está em IStripeGateway.
/// </summary>
public interface IPaymentGateway
{
    Task<string> CreatePaymentIntentAsync(Guid orderId, decimal amount, CancellationToken ct = default);
    Task<bool> CapturePaymentAsync(string paymentReference, CancellationToken ct = default);
}
