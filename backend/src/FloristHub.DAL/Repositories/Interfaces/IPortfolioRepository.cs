using FloristHub.DAL.Entities;

namespace FloristHub.DAL.Repositories.Interfaces;

public interface IPortfolioRepository : IRepository<PortfolioArrangement>
{
    IQueryable<PortfolioArrangement> QueryWithDetails();
    Task<PortfolioArrangement?> GetByIdWithDetailsForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
