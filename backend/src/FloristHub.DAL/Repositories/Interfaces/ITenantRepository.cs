using FloristHub.DAL.Entities;

namespace FloristHub.DAL.Repositories.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByIdWithSettingsForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByIdWithSettingsReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> CustomDomainExistsAsync(string customDomain, Guid? exceptTenantId = null, CancellationToken cancellationToken = default);
    Task<Tenant?> GetBySlugReadOnlyAsync(string slug, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByCustomDomainReadOnlyAsync(string customDomain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> ListActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> ListAllReadOnlyAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ListCustomDomainsReadOnlyAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> CountOrdersByTenantAsync(CancellationToken cancellationToken = default);
}
