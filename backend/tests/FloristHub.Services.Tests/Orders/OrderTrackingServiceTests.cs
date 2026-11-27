using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Orders;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Tests.Orders;

public class OrderTrackingServiceTests
{
  private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string Token = "cccccccccccccccccccccccccccccccccccccccccccc";

    [Fact]
    public async Task Returns_tracking_only_for_matching_tenant_and_token()
    {
        var tenant = new TenantContext();
        tenant.SetTenant(TenantA, "loja-a");
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        var order = new Order
        {
            TenantId = TenantA,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.InPreparation,
            DeliveryType = DeliveryType.HomeDelivery,
            TrackingAccessToken = Token,
            CapacityServiceDate = new DateOnly(2026, 11, 12),
            CapacityWindowKey = "09:00-13:00",
            EstimatedTotal = 20m,
        };
        order.StatusHistory.Add(new OrderStatusHistory
        {
            TenantId = TenantA,
            FromStatus = OrderStatus.PendingConfirmation,
            ToStatus = OrderStatus.Confirmed,
            ChangedByUserId = order.CustomerId,
        });
        db.DeliveryCapacityWindows.Add(new DeliveryCapacityWindow
        {
            TenantId = TenantA,
            Label = "Manhã",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(13, 0),
            OrderLimit = 5,
            SortOrder = 0,
        });
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sut = new OrderTrackingService(new UnitOfWork(db), tenant);
        var row = await sut.GetByTokenAsync(Token, CancellationToken.None);
        Assert.NotNull(row);
        Assert.Equal(order.Id, row!.OrderId);
        Assert.Equal("Manhã", row.DeliveryWindowLabel);
        Assert.Equal(OrderStatus.InPreparation, row.Status);

        tenant.SetTenant(Guid.NewGuid(), "outra");
        Assert.Null(await sut.GetByTokenAsync(Token, CancellationToken.None));
        Assert.Null(await sut.GetByTokenAsync("invalid", CancellationToken.None));
    }
}
