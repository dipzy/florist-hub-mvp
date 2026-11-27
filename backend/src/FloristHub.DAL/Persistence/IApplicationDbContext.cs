using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.DAL.Persistence;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Flower> Flowers { get; }
    DbSet<PortfolioArrangement> PortfolioArrangements { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderChangeProposal> OrderChangeProposals { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
