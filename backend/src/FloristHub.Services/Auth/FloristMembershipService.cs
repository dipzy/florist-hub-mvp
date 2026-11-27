using FloristHub.DAL.Constants;
using FloristHub.DAL.Entities;
using FloristHub.DAL.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Auth;

public interface IFloristMembershipService
{
    Task EnsureMembershipAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task<bool> HasMembershipAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task<ApplicationUser?> FindFloristUserForTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Tenant>> ListFloristTenantsAsync(Guid userId, CancellationToken ct = default);
}

public sealed class FloristMembershipService : IFloristMembershipService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _users;

    public FloristMembershipService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> users)
    {
        _unitOfWork = unitOfWork;
        _users = users;
    }

    public async Task EnsureMembershipAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        var exists = await _unitOfWork.FloristTenantMemberships.Query()
            .AnyAsync(m => m.UserId == userId && m.TenantId == tenantId, ct);
        if (exists)
            return;

        await _unitOfWork.FloristTenantMemberships.AddAsync(new FloristTenantMembership
        {
            UserId = userId,
            TenantId = tenantId,
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task<bool> HasMembershipAsync(Guid userId, Guid tenantId, CancellationToken ct = default) =>
        _unitOfWork.FloristTenantMemberships.Query()
            .AnyAsync(m => m.UserId == userId && m.TenantId == tenantId, ct);

    public async Task<ApplicationUser?> FindFloristUserForTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var floristUserIds = await _unitOfWork.FloristTenantMemberships.Query()
            .Where(m => m.TenantId == tenantId)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        foreach (var userId in floristUserIds)
        {
            var user = await _users.FindByIdAsync(userId.ToString());
            if (user is not null && await _users.IsInRoleAsync(user, Roles.Florist))
                return user;
        }

        var legacy = await _users.Users.Where(u => u.TenantId == tenantId).ToListAsync(ct);
        foreach (var user in legacy)
        {
            if (await _users.IsInRoleAsync(user, Roles.Florist))
                return user;
        }

        return null;
    }

    public async Task<IReadOnlyList<Tenant>> ListFloristTenantsAsync(Guid userId, CancellationToken ct = default)
    {
        var tenantIds = await _unitOfWork.FloristTenantMemberships.Query()
            .Where(m => m.UserId == userId)
            .Select(m => m.TenantId)
            .ToListAsync(ct);

        var user = await _users.FindByIdAsync(userId.ToString());
        if (user?.TenantId is Guid legacyTenant && !tenantIds.Contains(legacyTenant))
            tenantIds.Add(legacyTenant);

        if (tenantIds.Count == 0)
            return [];

        return await _unitOfWork.Tenants.Query()
            .Where(t => tenantIds.Contains(t.Id))
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }
}
