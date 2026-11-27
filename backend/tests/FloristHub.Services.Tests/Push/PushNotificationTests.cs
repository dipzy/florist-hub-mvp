using FloristHub.DAL.Audit;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.Services.Push;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Tests.Push;

public class FcmErrorClassifierTests
{
    [Theory]
    [InlineData(200, null, PushDeliveryOutcome.Sent)]
    [InlineData(404, """{"error":{"status":"NOT_FOUND","message":"Requested entity was not found."}}""", PushDeliveryOutcome.InvalidToken)]
    [InlineData(400, """{"error":{"status":"INVALID_ARGUMENT","message":"The registration token is not a valid FCM registration token","details":[{"errorCode":"INVALID_ARGUMENT"}]}}""", PushDeliveryOutcome.InvalidToken)]
    [InlineData(400, """{"error":{"status":"INVALID_ARGUMENT","message":"Request contains an invalid argument."}}""", PushDeliveryOutcome.Drop)]
    [InlineData(401, """{"error":{"status":"UNAUTHENTICATED","message":"Auth"}}""", PushDeliveryOutcome.Drop)]
    [InlineData(503, """{"error":{"status":"UNAVAILABLE","message":"later"}}""", PushDeliveryOutcome.Retry)]
    public void Classifies_fcm_responses(int status, string? body, PushDeliveryOutcome expected) =>
        Assert.Equal(expected, FcmErrorClassifier.Classify(status, body));

    [Fact]
    public void Unregistered_detail_removes_token_even_on_400()
    {
        const string body = """{"error":{"code":400,"status":"INVALID_ARGUMENT","details":[{"errorCode":"UNREGISTERED"}]}}""";
        Assert.Equal(PushDeliveryOutcome.InvalidToken, FcmErrorClassifier.Classify(400, body));
    }
}

public class OrderPushNotificationsTests
{
    [Fact]
    public void Enqueues_florist_and_customer_events_and_skips_cancelled()
    {
        var queue = new ListPushQueue();
        var sut = new OrderPushNotifications(queue, NullLogger<OrderPushNotifications>.Instance);
        var order = new Order { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), CustomerId = Guid.NewGuid() };
        var tenant = new Tenant { Id = order.TenantId, Name = "Loja", Slug = "loja" };

        sut.OrderCreated(order, tenant);
        sut.OrderPaid(order, tenant);
        sut.OrderStatusChanged(order, tenant, OrderStatus.Confirmed);
        sut.OrderStatusChanged(order, tenant, OrderStatus.InPreparation);
        sut.OrderStatusChanged(order, tenant, OrderStatus.Ready);
        sut.OrderStatusChanged(order, tenant, OrderStatus.Delivered);
        sut.OrderStatusChanged(order, tenant, OrderStatus.Cancelled);

        Assert.Equal(
            [
                NotificationPreferenceKind.NewOrder,
                NotificationPreferenceKind.OrderPaid,
                NotificationPreferenceKind.OrderStatus,
                NotificationPreferenceKind.OrderStatus,
                NotificationPreferenceKind.OrderStatus,
                NotificationPreferenceKind.OrderStatus
            ],
            queue.Items.Select(i => i.Kind).ToArray());
        Assert.Equal(PushAudience.TenantFlorists, queue.Items[0].Audience);
        Assert.Equal(PushAudience.Customer, queue.Items[2].Audience);
        Assert.Equal("Encomenda pronta", queue.Items[4].Title);
        Assert.All(queue.Items, i => Assert.Equal(order.TenantId, i.TenantId));
    }
}

public class PushDeliveryTests
{
    [Fact]
    public async Task New_order_reaches_only_florists_of_that_tenant()
    {
        var (db, floristA, _, customer, deviceA, deviceB, _) = await SeedAsync();
        var sender = new CaptureSender();
        var sut = Create(db, sender);
        var orderId = Guid.NewGuid();

        await sut.DeliverAsync(Item(deviceA.TenantId, PushAudience.TenantFlorists, null, orderId, NotificationPreferenceKind.NewOrder));

        var sent = Assert.Single(sender.Sent);
        Assert.Equal(deviceA.Token, sent.DeviceToken);
        Assert.NotEqual(deviceB.Token, sent.DeviceToken);
        Assert.Equal(orderId, sent.OrderId);
        _ = floristA;
        _ = customer;
    }

    [Fact]
    public async Task Disabled_preference_suppresses_the_customer_status()
    {
        var (db, _, _, customer, _, _, _) = await SeedAsync();
        db.NotificationPreferences.Add(new NotificationPreference
        {
            TenantId = customer.TenantId!.Value,
            UserId = customer.Id,
            OrderStatus = false
        });
        await db.SaveChangesAsync();
        var sender = new CaptureSender();

        await Create(db, sender).DeliverAsync(Item(
            customer.TenantId!.Value,
            PushAudience.Customer,
            customer.Id,
            Guid.NewGuid(),
            NotificationPreferenceKind.OrderStatus));

        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task Invalid_token_is_removed_and_sibling_token_stays()
    {
        var (db, floristA, _, _, deviceA, _, _) = await SeedAsync();
        var other = new PushDevice
        {
            TenantId = deviceA.TenantId,
            UserId = floristA.Id,
            Token = "outro-token-valido-123",
            Platform = PushPlatform.Android
        };
        db.PushDevices.Add(other);
        await db.SaveChangesAsync();
        var sender = new CaptureSender { InvalidTokens = { deviceA.Token } };

        await Create(db, sender).DeliverAsync(Item(
            deviceA.TenantId,
            PushAudience.TenantFlorists,
            null,
            Guid.NewGuid(),
            NotificationPreferenceKind.NewOrder));

        var remaining = await db.PushDevices.IgnoreQueryFilters().Where(d => d.UserId == floristA.Id).ToListAsync();
        Assert.DoesNotContain(remaining, d => d.Token == deviceA.Token);
        Assert.Contains(remaining, d => d.Token == other.Token);
    }

    [Fact]
    public async Task Transient_failure_retries_and_keeps_the_token()
    {
        var (db, _, _, _, deviceA, _, _) = await SeedAsync();
        var sender = new CaptureSender { Next = PushDeliveryOutcome.Retry };

        await Create(db, sender, attempts: 2).DeliverAsync(Item(
            deviceA.TenantId,
            PushAudience.TenantFlorists,
            null,
            Guid.NewGuid(),
            NotificationPreferenceKind.NewOrder));

        Assert.Equal(2, sender.Sent.Count);
        Assert.NotNull(await db.PushDevices.IgnoreQueryFilters().SingleAsync(d => d.Id == deviceA.Id));
    }

    private static PushDeliveryService Create(ApplicationDbContext db, CaptureSender sender, int attempts = 1) =>
        new(
            db,
            sender,
            Options.Create(new PushOptions { MaxSendAttempts = attempts }),
            new NullAudit(),
            NullLogger<PushDeliveryService>.Instance);

    private static PushWorkItem Item(Guid tenantId, PushAudience audience, Guid? userId, Guid orderId, NotificationPreferenceKind kind) =>
        new(tenantId, audience, userId, orderId, "Titulo", "Corpo", "tipo", kind);

    private static async Task<(ApplicationDbContext Db, ApplicationUser FloristA, ApplicationUser FloristB, ApplicationUser Customer, PushDevice DeviceA, PushDevice DeviceB, PushDevice CustomerDevice)> SeedAsync()
    {
        var tenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var tenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var tenantContext = new TenantContext();
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);

        var role = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = Roles.Florist, NormalizedName = "FLORISTA" };
        var floristA = new ApplicationUser { Id = Guid.NewGuid(), UserName = "a@loja.pt", Email = "a@loja.pt", TenantId = tenantA };
        var floristB = new ApplicationUser { Id = Guid.NewGuid(), UserName = "b@loja.pt", Email = "b@loja.pt", TenantId = tenantB };
        var customer = new ApplicationUser { Id = Guid.NewGuid(), UserName = "c@loja.pt", Email = "c@loja.pt", TenantId = tenantA };
        db.Roles.Add(role);
        db.Users.AddRange(floristA, floristB, customer);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = floristA.Id, RoleId = role.Id });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = floristB.Id, RoleId = role.Id });
        var deviceA = new PushDevice { TenantId = tenantA, UserId = floristA.Id, Token = "token-florista-a-1234", Platform = PushPlatform.Android };
        var deviceB = new PushDevice { TenantId = tenantB, UserId = floristB.Id, Token = "token-florista-b-1234", Platform = PushPlatform.Android };
        var customerDevice = new PushDevice { TenantId = tenantA, UserId = customer.Id, Token = "token-cliente-a-12345", Platform = PushPlatform.Ios };
        db.PushDevices.AddRange(deviceA, deviceB, customerDevice);
        await db.SaveChangesAsync();
        return (db, floristA, floristB, customer, deviceA, deviceB, customerDevice);
    }

    private sealed class CaptureSender : IPushSender
    {
        public List<PushMessage> Sent { get; } = [];
        public PushDeliveryOutcome Next { get; set; } = PushDeliveryOutcome.Sent;
        public HashSet<string> InvalidTokens { get; } = [];

        public Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            var outcome = InvalidTokens.Contains(message.DeviceToken) ? PushDeliveryOutcome.InvalidToken : Next;
            return Task.FromResult(new PushSendResult(outcome));
        }
    }

    private sealed class NullAudit : IAuditLogger
    {
        public Task LogAsync(string action, Guid? userId, Guid? tenantId, string? entityType = null, Guid? entityId = null, string? details = null, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}

public class PushDeviceServiceTests
{
    [Fact]
    public async Task Token_and_preferences_stay_inside_the_current_tenant()
    {
        var tenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var tenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var context = new TenantContext();
        context.SetTenant(tenantA, "a");
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            context);
        var sut = new PushDeviceService(db, context);
        const string token = "aparelho-partilhado-01";

        await sut.RegisterAsync(userA, token, "Android");
        context.SetTenant(tenantB, "b");
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.UnregisterAsync(userB, token));

        var defaults = await sut.GetPreferencesAsync(userB);
        Assert.True(defaults.NewOrder);

        context.SetTenant(tenantA, "a");
        await sut.UpdatePreferencesAsync(userA, new UpdateNotificationPreferencesRequest(false, true, true));

        context.SetTenant(tenantB, "b");
        Assert.True((await sut.GetPreferencesAsync(userB)).NewOrder);
        await sut.RegisterAsync(userB, token, "Ios");

        context.SetTenant(tenantA, "a");
        Assert.Empty(await db.PushDevices.ToListAsync());
        Assert.False((await sut.GetPreferencesAsync(userA)).NewOrder);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.UnregisterAsync(userA, token));

        var moved = await db.PushDevices.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(tenantB, moved.TenantId);
        Assert.Equal(userB, moved.UserId);
        Assert.Equal(PushPlatform.Ios, moved.Platform);
    }

    [Theory]
    [InlineData("", "Android")]
    [InlineData("curto", "Android")]
    [InlineData("token com espaco 123", "Android")]
    [InlineData("token-valido-123456", "Windows")]
    public async Task Rejects_bad_token_or_platform(string token, string platform)
    {
        var context = new TenantContext();
        context.SetTenant(Guid.NewGuid(), "loja");
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            context);
        var sut = new PushDeviceService(db, context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterAsync(Guid.NewGuid(), token, platform));
    }
}

public class PushOptionsTests
{
    [Fact]
    public void Fcm_without_credentials_is_not_ready()
    {
        Assert.False(new PushOptions { Provider = "Fcm", FcmProjectId = "proj" }.IsFcmReady());
        Assert.False(new PushOptions { Provider = "Log", FcmProjectId = "proj", FcmServiceAccountJson = "{}" }.IsFcmReady());
        Assert.True(new PushOptions { Provider = "Fcm", FcmProjectId = "proj", FcmServiceAccountJson = "{}" }.IsFcmReady());
    }
}

file sealed class ListPushQueue : IPushQueue
{
    public List<PushWorkItem> Items { get; } = [];
    public void Enqueue(PushWorkItem item) => Items.Add(item);
}
