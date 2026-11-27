using FloristHub.DAL.Persistence;
using Microsoft.Extensions.Configuration;

namespace FloristHub.Services.Tests.Persistence;

public class ConnectionStringFactoryTests
{
    [Fact]
    public void Neon_sem_ssl_passa_a_require()
    {
        var configuration = Config(("ConnectionStrings:DefaultConnection", "Host=ep-abc.eu-central-1.aws.neon.tech;Database=neondb;Username=u;Password=p"));
        var connection = ConnectionStringFactory.Resolve(configuration);
        Assert.Contains("SSL Mode=Require", connection, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Timeout=30", connection, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Local_mantem_ssl_desligado()
    {
        var configuration = Config(("ConnectionStrings:DefaultConnection", "Host=localhost;Port=5432;Database=floristhub;Username=floristhub;Password=floristhub;SSL Mode=Disable"));
        var connection = ConnectionStringFactory.Resolve(configuration);
        Assert.Contains("SSL Mode=Disable", connection, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pooler_desliga_prepare_e_multiplexing()
    {
        var configuration = Config(("DATABASE_URL", "postgresql://u:p@ep-abc-pooler.eu-central-1.aws.neon.tech/neondb?sslmode=require"));
        var connection = ConnectionStringFactory.Resolve(configuration);
        Assert.Contains("Max Auto Prepare=0", connection, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No Reset On Close=True", connection, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Multiplexing=True", connection, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migracoes_preferem_a_ligacao_directa()
    {
        var configuration = Config(
            ("ConnectionStrings:DefaultConnection", "Host=ep-abc-pooler.eu-central-1.aws.neon.tech;Database=neondb;Username=u;Password=p"),
            ("DATABASE_URL_UNPOOLED", "Host=ep-abc.eu-central-1.aws.neon.tech;Database=neondb;Username=u;Password=p;SSL Mode=Require"));
        var runtime = ConnectionStringFactory.Resolve(configuration);
        var migrations = ConnectionStringFactory.ResolveForMigrations(configuration);
        Assert.Contains("-pooler", runtime, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("-pooler", migrations, StringComparison.OrdinalIgnoreCase);
    }

    private static IConfiguration Config(params (string Key, string Value)[] pairs)
    {
        var values = pairs.ToDictionary(pair => pair.Key, pair => (string?)pair.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
