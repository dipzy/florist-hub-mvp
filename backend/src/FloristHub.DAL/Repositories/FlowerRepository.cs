using FloristHub.DAL.Entities;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.DAL.Repositories;

public class FlowerRepository : Repository<Flower>, IFlowerRepository
{
    public FlowerRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Flower>> ListOrderedByNameAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().OrderBy(f => f.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> FilterExistingIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return Array.Empty<Guid>();

        return await Set.Where(f => ids.Contains(f.Id)).Select(f => f.Id).ToListAsync(cancellationToken);
    }

    public Task<bool> IsReferencedInOrdersAsync(Guid flowerId, CancellationToken cancellationToken = default) =>
        Context.OrderItems.AsNoTracking().AnyAsync(i => i.FlowerId == flowerId, cancellationToken);

    public async Task<(IReadOnlyList<Flower> Items, int Total)> SearchPagedAsync(
        string? search,
        bool? catalogActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = Set.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(f =>
                f.Name.ToLower().Contains(term) ||
                (f.Color != null && f.Color.ToLower().Contains(term)) ||
                (f.Notes != null && f.Notes.ToLower().Contains(term)));
        }

        if (catalogActive.HasValue)
            query = query.Where(f => f.IsCatalogActive == catalogActive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
