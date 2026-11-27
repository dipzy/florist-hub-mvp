using FloristHub.DAL.Entities;
using FloristHub.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Email;

public interface IFloristInviteStore
{
    Task<FloristInvite> IssueAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task RevokeActiveForUserAsync(Guid userId, CancellationToken ct = default);
    Task<FloristInvite?> GetActiveInviteAsync(Guid userId, CancellationToken ct = default);
    Task MarkUsedAsync(Guid userId, CancellationToken ct = default);
}

public sealed class FloristInviteStore : IFloristInviteStore
{
    private readonly IUnitOfWork _unitOfWork;

    public FloristInviteStore(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<FloristInvite> IssueAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        await RevokeActiveForUserAsync(userId, ct);
        var invite = new FloristInvite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(FloristInviteEmailComposer.InviteValidHours),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _unitOfWork.Add(invite);
        await _unitOfWork.SaveChangesAsync(ct);
        return invite;
    }

    public async Task RevokeActiveForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var active = await _unitOfWork.FloristInvites.Query()
            .Where(i => i.UserId == userId && i.RevokedAt == null && i.UsedAt == null)
            .ToListAsync(ct);
        foreach (var invite in active)
            invite.RevokedAt = now;
        if (active.Count > 0)
            await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task<FloristInvite?> GetActiveInviteAsync(Guid userId, CancellationToken ct = default) =>
        _unitOfWork.FloristInvites.Query()
            .Where(i => i.UserId == userId && i.RevokedAt == null && i.UsedAt == null)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task MarkUsedAsync(Guid userId, CancellationToken ct = default)
    {
        var invite = await GetActiveInviteAsync(userId, ct);
        if (invite is null)
            return;
        invite.UsedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
