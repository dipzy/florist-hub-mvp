using FloristHub.DAL.Entities;

namespace FloristHub.DAL.Repositories.Interfaces;

public interface IFlowerRepository : IRepository<Flower>
{
    Task<IReadOnlyList<Flower>> ListOrderedByNameAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> FilterExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> IsReferencedInOrdersAsync(Guid flowerId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Flower> Items, int Total)> SearchPagedAsync(
        string? search,
        bool? catalogActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
