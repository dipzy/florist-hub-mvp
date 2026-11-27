using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Tests.Notifications;

public class FloristPanelNotificationServiceTests
{
    private static readonly Guid TenantA = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Records_new_orders_and_marks_read_per_tenant()
    {
        var tenant = new TenantContext();
        tenant.SetTenant(TenantA, "loja-a");
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        var orderId = Guid.NewGuid();
        db.Orders.Add(new Order
        {
            Id = orderId,
            TenantId = TenantA,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.PendingConfirmation,
            DeliveryType = DeliveryType.StorePickup,
            EstimatedTotal = 12m,
        });
        await db.SaveChangesAsync();

        var sut = new FloristPanelNotificationService(new UnitOfWork(db), tenant);
        await sut.RecordNewOrderAsync(TenantA, orderId, CancellationToken.None);
        await sut.RecordNewOrderAsync(TenantA, orderId, CancellationToken.None);

        var summary = await sut.GetSummaryAsync(CancellationToken.None);
        Assert.Equal(1, summary.UnreadCount);

        var list = await sut.ListUnreadAsync(10, CancellationToken.None);
        Assert.Single(list);
        Assert.Equal(FloristPanelNotificationKind.NewOrder, list[0].Kind);

        await sut.MarkAllReadAsync(CancellationToken.None);
        Assert.Equal(0, (await sut.GetSummaryAsync(CancellationToken.None)).UnreadCount);

        tenant.SetTenant(Guid.NewGuid(), "outra");
        Assert.Equal(0, (await sut.GetSummaryAsync(CancellationToken.None)).UnreadCount);
    }
}
