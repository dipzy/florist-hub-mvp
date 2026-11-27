using FloristHub.DAL.Entities;

namespace FloristHub.DAL.Repositories.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetLatestForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> ListForCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<Payment?> GetWithEventsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> FindForWebhookAsync(Guid? paymentId, string? sessionId, string? paymentIntentId, CancellationToken cancellationToken = default);
}
