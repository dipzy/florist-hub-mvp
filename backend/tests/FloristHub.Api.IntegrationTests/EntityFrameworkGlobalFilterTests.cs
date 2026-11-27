using FloristHub.DAL.Configurations;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FloristHub.Api.IntegrationTests;

[Collection(PostgresApiCollection.Name)]
public sealed class EntityFrameworkGlobalFilterTests
{
    private readonly FloristHubPostgresFixture _fixture;

    public EntityFrameworkGlobalFilterTests(FloristHubPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public void Tenant_scoped_entities_have_global_query_filters()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var model = db.Model;

        foreach (var type in TenantQueryFilterConfigurer.TenantScopedEntityTypes)
        {
            var entity = model.FindEntityType(type);
            Assert.NotNull(entity);
            Assert.NotNull(entity.GetQueryFilter());
        }
    }

    [Fact]
    public async Task Query_filter_hides_other_tenant_rows()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var tenantContext = (TenantContext)scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(ApiTestClient.Demo.TenantLina);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var flowers = await db.Flowers.AsNoTracking().Select(f => f.TenantId).Distinct().ToListAsync();
        Assert.Single(flowers);
        Assert.Equal(ApiTestClient.Demo.TenantLina, flowers[0]);

        tenantContext.SetTenant(ApiTestClient.Demo.TenantCampo);
        var campoFlowers = await db.Flowers.AsNoTracking().ToListAsync();
        Assert.All(campoFlowers, f => Assert.Equal(ApiTestClient.Demo.TenantCampo, f.TenantId));
        Assert.DoesNotContain(campoFlowers, f => f.Id == ApiTestClient.Demo.RoseLina);
    }
}
