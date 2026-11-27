using FloristHub.DAL.Entities;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Tests.Onboarding;

public class FloristInviteStoreTests
{
    [Fact]
    public async Task Resend_revokes_previous_active_invite()
    {
        await using var db = CreateDb();
        var unit = new UnitOfWork(db);
        var store = new FloristInviteStore(unit);
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await store.IssueAsync(userId, tenantId);
        await store.IssueAsync(userId, tenantId);

        var active = await db.FloristInvites
            .Where(i => i.UserId == userId && i.RevokedAt == null && i.UsedAt == null)
            .CountAsync();
        Assert.Equal(1, active);

        var revoked = await db.FloristInvites.CountAsync(i => i.UserId == userId && i.RevokedAt != null);
        Assert.Equal(1, revoked);
    }

    [Fact]
    public async Task Expired_invite_fails_active_lookup_when_used()
    {
        await using var db = CreateDb();
        var unit = new UnitOfWork(db);
        var store = new FloristInviteStore(unit);
        var userId = Guid.NewGuid();
        var invite = await store.IssueAsync(userId, Guid.NewGuid());
        invite.ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();

        var active = await store.GetActiveInviteAsync(userId);
        Assert.NotNull(active);
        Assert.True(active!.ExpiresAt < DateTimeOffset.UtcNow);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new FloristHub.DAL.MultiTenancy.TenantContext());
    }
}
