using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Repositories.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    IQueryable<Order> QueryWithDetails();
    IQueryable<Order> QueryWithDetailsForFlorist();
    Task<(IReadOnlyList<Order> Items, int Total)> SearchForFloristPagedAsync(
        OrderStatus? status,
        DeliveryType? deliveryType,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? customerSearch,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdWithStatusHistoryForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdForProposalResponseForUpdateAsync(Guid orderId, CancellationToken cancellationToken = default);
}
