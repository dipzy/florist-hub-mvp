using FloristHub.DAL.Entities;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.DAL.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context) : base(context) { }

    public Task<Payment?> GetLatestForOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        Set.Include(p => p.Events)
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Payment>> ListForCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(p => p.Events)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Payment?> GetWithEventsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(p => p.Events).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Payment?> FindForWebhookAsync(
        Guid? paymentId,
        string? sessionId,
        string? paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        var query = Set.IgnoreQueryFilters().Include(p => p.Events);
        if (paymentId is Guid id)
            return query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(sessionId))
            return query.FirstOrDefaultAsync(p => p.StripeCheckoutSessionId == sessionId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(paymentIntentId))
            return query.FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);
        return Task.FromResult<Payment?>(null);
    }
}
