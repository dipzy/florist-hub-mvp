using FloristHub.DAL.Entities;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.DAL.Repositories;

public class PortfolioRepository : Repository<PortfolioArrangement>, IPortfolioRepository
{
    public PortfolioRepository(ApplicationDbContext context) : base(context) { }

    public IQueryable<PortfolioArrangement> QueryWithDetails() =>
        Set.AsNoTracking()
            .Include(p => p.Photos.OrderBy(x => x.SortOrder))
            .Include(p => p.Items);

    public async Task<PortfolioArrangement?> GetByIdWithDetailsForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await Set
            .Include(p => p.Photos)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(p => p.Id == id, cancellationToken);
}
