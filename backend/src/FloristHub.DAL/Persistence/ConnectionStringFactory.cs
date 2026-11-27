using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FloristHub.DAL.Persistence;

/// <summary>
/// Resolve a connection string a partir de <c>ConnectionStrings:DefaultConnection</c> ou <c>DATABASE_URL</c>
/// (URI <c>postgres://</c>). No Neon força SSL e, no host <c>-pooler</c>, desliga funcionalidades incompatíveis com o PgBouncer.
/// As migrações preferem <c>ConnectionStrings:Migrations</c> ou <c>DATABASE_URL_UNPOOLED</c> (ligação directa).
/// </summary>
public static class ConnectionStringFactory
{
    public static string Resolve(IConfiguration configuration) =>
        Normalize(Read(FirstNonEmpty(
            configuration.GetConnectionString("DefaultConnection"),
            configuration["DATABASE_URL"],
            Environment.GetEnvironmentVariable("DATABASE_URL"))));

    public static string ResolveForMigrations(IConfiguration configuration)
    {
        var direct = FirstNonEmpty(
            configuration.GetConnectionString("Migrations"),
            configuration["DATABASE_URL_UNPOOLED"],
            Environment.GetEnvironmentVariable("DATABASE_URL_UNPOOLED"));
        if (!string.IsNullOrWhiteSpace(direct))
            return Normalize(Read(direct));
        return Resolve(configuration);
    }

    private static string Read(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                "Connection string em falta. Defina ConnectionStrings__DefaultConnection ou DATABASE_URL.");

        if (value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return FromPostgresUri(value);

        return value;
    }

    private static string FromPostgresUri(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("postgres" or "postgresql"))
            throw new InvalidOperationException("DATABASE_URL deve ser uma URI postgres:// ou postgresql://");

        var userInfo = Uri.UnescapeDataString(uri.UserInfo);
        var split = userInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.Trim('/'),
            Username = split[0],
        };
        if (split.Length == 2)
            builder.Password = split[1];

        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            var key = Uri.UnescapeDataString(eq >= 0 ? pair[..eq] : pair);
            var raw = Uri.UnescapeDataString(eq >= 0 ? pair[(eq + 1)..] : string.Empty);
            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                builder.SslMode = ParseSslMode(raw);
        }

        if (string.IsNullOrWhiteSpace(builder.Database))
            throw new InvalidOperationException("DATABASE_URL sem nome de base de dados.");

        return builder.ConnectionString;
    }

    private static SslMode ParseSslMode(string value) => value.ToLowerInvariant() switch
    {
        "disable" => SslMode.Disable,
        "allow" => SslMode.Allow,
        "prefer" or "" => SslMode.Prefer,
        "require" => SslMode.Require,
        "verify-ca" => SslMode.VerifyCA,
        "verify-full" => SslMode.VerifyFull,
        _ => throw new InvalidOperationException($"sslmode '{value}' não é reconhecido.")
    };

    private static string Normalize(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var host = builder.Host ?? string.Empty;
        var neon = host.Contains("neon.tech", StringComparison.OrdinalIgnoreCase);
        if (neon && builder.SslMode < SslMode.Require)
            builder.SslMode = SslMode.Require;

        if (host.Contains("-pooler", StringComparison.OrdinalIgnoreCase))
        {
            builder.Multiplexing = false;
            builder.MaxAutoPrepare = 0;
            builder.NoResetOnClose = true;
        }

        if (neon && builder.Timeout < 30)
            builder.Timeout = 30;

        // A imagem Alpine não traz GSSAPI. Sem isto o Npgsql escreve um erro ao arrancar.
        builder.GssEncryptionMode = GssEncryptionMode.Disable;

        return builder.ConnectionString;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
