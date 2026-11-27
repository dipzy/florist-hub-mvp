using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Common;
using FloristHub.Services.DTOs.Florist;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services;

public interface IFloristCustomersService
{
    Task<PagedResult<FloristCustomerDto>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default);
}

public class FloristCustomersService : IFloristCustomersService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;

    public FloristCustomersService(IUnitOfWork unitOfWork, ITenantContext tenant)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
    }

    public async Task<PagedResult<FloristCustomerDto>> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        RequireTenant();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var orders = _unitOfWork.Orders.Query().AsNoTracking();

        IQueryable<DAL.Entities.Order> scoped = orders;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            scoped = orders.Where(o =>
                (o.Customer != null && (
                    (o.Customer.FullName != null && o.Customer.FullName.ToLower().Contains(term)) ||
                    (o.Customer.Email != null && o.Customer.Email.ToLower().Contains(term)))));
        }

        var aggregated = scoped.GroupBy(o => o.CustomerId).Select(g => new
        {
            CustomerId = g.Key,
            OrderCount = g.Count(),
            TotalSpent = g.Sum(o => o.EstimatedTotal),
        });

        var total = await aggregated.CountAsync(ct);
        var stats = await aggregated
            .OrderByDescending(x => x.OrderCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (stats.Count == 0)
            return new PagedResult<FloristCustomerDto>([], total, page, pageSize);

        var customerIds = stats.Select(s => s.CustomerId).ToList();
        var customerRows = await orders
            .Where(o => customerIds.Contains(o.CustomerId))
            .Select(o => new { o.CustomerId, FullName = o.Customer!.FullName, Email = o.Customer!.Email })
            .Distinct()
            .ToListAsync(ct);
        var customersById = customerRows.ToDictionary(c => c.CustomerId);

        var items = stats
            .Select(s =>
            {
                customersById.TryGetValue(s.CustomerId, out var customer);
                return new FloristCustomerDto(
                    s.CustomerId,
                    customer?.FullName,
                    customer?.Email,
                    s.OrderCount,
                    s.TotalSpent);
            })
            .ToList();

        return new PagedResult<FloristCustomerDto>(items, total, page, pageSize);
    }

    private void RequireTenant()
    {
        if (!_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Tenant não resolvido.");
    }
}
