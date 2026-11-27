using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FloristHub.Api.IntegrationTests;

public sealed class FloristHubPostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(fromEnv))
            fromEnv = Environment.GetEnvironmentVariable("FLORISTHUB_TEST_PG");
        ConnectionString = string.IsNullOrWhiteSpace(fromEnv) ? string.Empty : fromEnv;

        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("floristhub_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Factory = CreateFactory();

        using var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.GetAsync("/health");
    }

    public WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "TestSmartAuth";
                        options.DefaultChallengeScheme = "TestSmartAuth";
                    })
                    .AddPolicyScheme("TestSmartAuth", "TestSmartAuth", policy =>
                    {
                        policy.ForwardDefaultSelector = context =>
                        {
                            var header = context.Request.Headers.Authorization.ToString();
                            if (!string.IsNullOrWhiteSpace(header) &&
                                header.StartsWith(IntegrationTestAuthHandler.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
                                return IntegrationTestAuthHandler.Scheme;
                            return JwtBearerDefaults.AuthenticationScheme;
                        };
                    })
                    .AddScheme<AuthenticationSchemeOptions, IntegrationTestAuthHandler>(
                        IntegrationTestAuthHandler.Scheme,
                        _ => { });
                });
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                        ["Database:MigrateOnStartup"] = "true",
                        ["Jwt:Key"] = "FloristHub_Test_Signing_Key_Min_32_Chars",
                        ["App:BaseDomain"] = "floristhub.test",
                        ["Cors:Origins:0"] = "http://localhost:5173",
                        ["Email:Provider"] = "Log",
                        ["Features:DisableRateLimiting"] = "true",
                        ["Jwt:Issuer"] = "FloristHub",
                        ["Jwt:Audience"] = "FloristHubClients",
                        ["Stripe:Provider"] = "Fake",
                    });
                });
            });

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
