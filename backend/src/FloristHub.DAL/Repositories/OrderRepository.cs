using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.DAL.Repositories;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }

    public IQueryable<Order> QueryWithDetails() =>
        Set.AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.DeliveryZone)
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .Include(o => o.ChangeProposals)
            .Include(o => o.Payments)
            .Include(o => o.Review);

    public IQueryable<Order> QueryWithDetailsForFlorist() =>
        QueryWithDetails()
            .Include(o => o.Customer);

    public async Task<(IReadOnlyList<Order> Items, int Total)> SearchForFloristPagedAsync(
        OrderStatus? status,
        DeliveryType? deliveryType,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? customerSearch,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = QueryWithDetailsForFlorist();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);
        if (deliveryType.HasValue)
            query = query.Where(o => o.DeliveryType == deliveryType.Value);
        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(customerSearch))
        {
            var term = customerSearch.Trim().ToLower();
            query = query.Where(o =>
                (o.Customer != null && (
                    (o.Customer.FullName != null && o.Customer.FullName.ToLower().Contains(term)) ||
                    (o.Customer.Email != null && o.Customer.Email.ToLower().Contains(term)))));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        QueryWithDetails().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetByIdWithStatusHistoryForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetByIdForProposalResponseForUpdateAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        Set
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .Include(o => o.ChangeProposals)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
}
