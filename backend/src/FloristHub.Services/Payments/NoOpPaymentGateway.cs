using FloristHub.Services.Interfaces.Payments;

namespace FloristHub.Services.Payments;

public class NoOpPaymentGateway : IPaymentGateway
{
    public Task<string> CreatePaymentIntentAsync(Guid orderId, decimal amount, CancellationToken ct = default) =>
        Task.FromResult($"stub-payment-{orderId:N}");

    public Task<bool> CapturePaymentAsync(string paymentReference, CancellationToken ct = default) =>
        Task.FromResult(true);
}
