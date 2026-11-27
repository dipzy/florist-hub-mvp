using FloristHub.DAL.Entities;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.DAL.Repositories;

public class TenantRepository : Repository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Tenant?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Tenant?> GetByIdWithSettingsForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set
            .Include(t => t.OpeningHours)
            .Include(t => t.DeliveryZones)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Tenant?> GetByIdWithSettingsReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(t => t.OpeningHours)
            .Include(t => t.DeliveryZones)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(t => t.Slug == slug, cancellationToken);

    public Task<bool> CustomDomainExistsAsync(string customDomain, Guid? exceptTenantId = null, CancellationToken cancellationToken = default)
    {
        var q = Set.Where(t => t.CustomDomain == customDomain);
        if (exceptTenantId is Guid id)
            q = q.Where(t => t.Id != id);
        return q.AnyAsync(cancellationToken);
    }

    public Task<Tenant?> GetBySlugReadOnlyAsync(string slug, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

    public Task<Tenant?> GetByCustomDomainReadOnlyAsync(string customDomain, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(t => t.CustomDomain == customDomain, cancellationToken);

    public async Task<IReadOnlyList<Tenant>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(t => t.IsActive && t.IsPublished)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Tenant>> ListAllReadOnlyAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> ListCustomDomainsReadOnlyAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(t => t.CustomDomain != null && t.CustomDomain != "")
            .Select(t => t.CustomDomain!)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<Guid, int>> CountOrdersByTenantAsync(CancellationToken cancellationToken = default)
    {
        var counts = await Context.Orders
            .IgnoreQueryFilters()
            .GroupBy(o => o.TenantId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return counts.ToDictionary(x => x.Key, x => x.Count);
    }
}
