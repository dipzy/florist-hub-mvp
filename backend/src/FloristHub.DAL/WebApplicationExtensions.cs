using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.Seed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FloristHub.DAL;

public static class WebApplicationExtensions
{
    private const int MigrationLockKey = 824611;

    public static async Task ApplyFloristHubDatabaseAsync(this WebApplication app, bool forceMigrate = false)
    {
        var configuration = app.Configuration;
        var migrate = forceMigrate || configuration.GetValue("Database:MigrateOnStartup", app.Environment.IsDevelopment());

        using var scope = app.Services.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FloristHub.Database");
        if (migrate)
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var migrationConnection = ConnectionStringFactory.ResolveForMigrations(configuration);
            db.Database.SetConnectionString(migrationConnection);
            if (migrationConnection.Contains("-pooler", StringComparison.OrdinalIgnoreCase))
            {
                log.LogWarning(
                    "As migrações estão a usar um host -pooler do Neon. Defina DATABASE_URL_UNPOOLED com a ligação directa.");
            }

            await MigrateWithLockAsync(db, log);
            await BackfillOrderTrackingTokensAsync(db, log);
        }
        else
        {
            log.LogInformation("Migrações no arranque desligadas (Database__MigrateOnStartup=false).");
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { Roles.SuperAdmin, Roles.Florist, Roles.Customer })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = role });
        }

        await MigrateLegacyPlatformAdminRoleAsync(roleManager);

        DevelopmentDemoAssetsInstaller.EnsureInstalled(app.Environment, configuration, log);
        await DevelopmentDataSeeder.SeedAsync(app.Services, app.Environment);
    }

    private static async Task MigrateWithLockAsync(ApplicationDbContext db, ILogger log)
    {
        var delay = TimeSpan.FromSeconds(2);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var connection = db.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                await ExecuteLockAsync(connection, "SELECT pg_advisory_lock(@k)");
                try
                {
                    log.LogInformation("A aplicar migrações EF. O primeiro acesso pode esperar que o Neon acorde.");
                    var watch = Stopwatch.StartNew();
                    db.Database.SetCommandTimeout(180);
                    await db.Database.MigrateAsync();
                    log.LogInformation("Migrações EF concluídas em {ElapsedMs} ms.", watch.ElapsedMilliseconds);
                }
                finally
                {
                    await ExecuteLockAsync(connection, "SELECT pg_advisory_unlock(@k)");
                }

                return;
            }
            catch (Exception ex) when (attempt < 5)
            {
                log.LogWarning(ex, "Ligação à base de dados falhou (tentativa {Attempt}/5).", attempt);
                await Task.Delay(delay);
                delay = TimeSpan.FromSeconds(Math.Min(10, delay.TotalSeconds * 2));
            }
        }
    }

    private static async Task ExecuteLockAsync(System.Data.Common.DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "k";
        parameter.Value = MigrationLockKey;
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task BackfillOrderTrackingTokensAsync(ApplicationDbContext db, ILogger log)
    {
        var missing = await db.Orders
            .Where(o => o.TrackingAccessToken == null || o.TrackingAccessToken == "")
            .Take(2000)
            .ToListAsync();
        if (missing.Count == 0)
            return;

        foreach (var order in missing)
            order.TrackingAccessToken = CreateTrackingAccessToken();

        await db.SaveChangesAsync();
        log.LogInformation("Backfill: {Count} encomendas receberam TrackingAccessToken.", missing.Count);
    }

    private static string CreateTrackingAccessToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static async Task MigrateLegacyPlatformAdminRoleAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        if (await roleManager.RoleExistsAsync(Roles.SuperAdmin))
            return;

        var legacy = await roleManager.FindByNameAsync("AdminPlataforma");
        if (legacy is null)
            return;

        await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = Roles.SuperAdmin });
    }
}
