using FloristHub.DAL.Audit;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FloristHub.Services.Tests.Legal;

public class DataSubjectRightsServiceTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Export_only_includes_orders_from_current_tenant()
    {
        var fx = Create(TenantA);
        var customer = await SeedCustomerAsync(fx, TenantA, "cliente-a@test.pt");
        fx.TenantContext.SetTenant(TenantA, "loja-a");
        fx.Db.Orders.Add(new Order
        {
            TenantId = TenantA,
            CustomerId = customer.Id,
            BaseType = ArrangementBaseType.Sponge,
            DeliveryType = DeliveryType.StorePickup,
            DeliveryAddress = "Morada A",
            EstimatedTotal = 20,
        });
        fx.Db.Orders.Add(new Order
        {
            TenantId = TenantB,
            CustomerId = customer.Id,
            BaseType = ArrangementBaseType.Sponge,
            DeliveryType = DeliveryType.StorePickup,
            EstimatedTotal = 99,
        });
        await fx.Db.SaveChangesAsync();

        var export = await fx.Service.ExportMyDataAsync(customer.Id);
        Assert.Single(export.Orders);
        Assert.Equal(20, export.Orders[0].EstimatedTotal);
    }

    [Fact]
    public async Task Anonymize_clears_pii_but_keeps_orders()
    {
        var fx = Create(TenantA);
        var customer = await SeedCustomerAsync(fx, TenantA, "maria@test.pt");
        await AssignCustomerRoleAsync(fx, customer);
        fx.Db.Orders.Add(new Order
        {
            TenantId = TenantA,
            CustomerId = customer.Id,
            BaseType = ArrangementBaseType.Sponge,
            DeliveryType = DeliveryType.HomeDelivery,
            DeliveryAddress = "Rua X",
            CustomerNotes = "Entregar à tarde",
            EstimatedTotal = 15,
        });
        await fx.Db.SaveChangesAsync();

        await fx.Service.AnonymizeMyAccountAsync(customer.Id);

        var user = await fx.UserManager.FindByIdAsync(customer.Id.ToString());
        Assert.True(user!.IsAnonymized);
        Assert.Contains("anon.floristhub.invalid", user.Email);
        var order = await fx.Db.Orders.IgnoreQueryFilters().SingleAsync(o => o.CustomerId == customer.Id);
        Assert.Null(order.DeliveryAddress);
        Assert.Equal("[anonimizado]", order.CustomerNotes);
    }

    private static async Task AssignCustomerRoleAsync(Fixture fx, ApplicationUser user)
    {
        var roleId = Guid.NewGuid();
        fx.Db.Roles.Add(new IdentityRole<Guid> { Id = roleId, Name = Roles.Customer, NormalizedName = Roles.Customer.ToUpperInvariant() });
        fx.Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roleId });
        await fx.Db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> SeedCustomerAsync(Fixture fx, Guid tenantId, string email)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = "Maria",
            TenantId = tenantId,
        };
        await fx.UserManager.CreateAsync(user, "Teste123!Aa");
        return user;
    }

    private static Fixture Create(Guid tenantId)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(tenantId, "loja-a");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options, tenantContext);
        db.Tenants.AddRange(
            new Tenant { Id = TenantA, Name = "A", Slug = "a" },
            new Tenant { Id = TenantB, Name = "B", Slug = "b" });
        db.SaveChanges();

        var uow = new UnitOfWork(db);
        var audit = new AuditLogger(uow, NullLogger<AuditLogger>.Instance);
        var userStore = new UserStore<ApplicationUser, IdentityRole<Guid>, ApplicationDbContext, Guid>(db);
        var userManager = new UserManager<ApplicationUser>(
            userStore,
            null!,
            new PasswordHasher<ApplicationUser>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);
        var service = new DataSubjectRightsService(userManager, uow, tenantContext, audit);
        return new Fixture(db, uow, tenantContext, userManager, service);
    }

    private sealed record Fixture(
        ApplicationDbContext Db,
        UnitOfWork Uow,
        TenantContext TenantContext,
        UserManager<ApplicationUser> UserManager,
        DataSubjectRightsService Service);
}
