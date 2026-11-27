using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using FloristHub.Api.Auth;
using FloristHub.Api.Configuration;
using FloristHub.Api.Errors;
using FloristHub.Api.Localization;
using FloristHub.Api.Middleware;
using FloristHub.DAL;
using FloristHub.DAL.Constants;
using FloristHub.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Sentry;
using Serilog;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

var migrateOnly = args.Contains("--migrate");
var builder = WebApplication.CreateBuilder(args.Where(a => !string.Equals(a, "--migrate", StringComparison.Ordinal)).ToArray());
if (migrateOnly && string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:Key"] = "migrate-only-placeholder-not-used-for-tokens",
    });
}

var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.SendDefaultPii = false;
        options.MaxRequestBodySize = Sentry.Extensibility.RequestSize.None;
        options.TracesSampleRate = builder.Configuration.GetValue("Sentry:TracesSampleRate", 0.1);
        options.Environment = builder.Configuration["Sentry:Environment"] ?? builder.Environment.EnvironmentName;
        var release = builder.Configuration["Sentry:Release"];
        if (!string.IsNullOrWhiteSpace(release))
            options.Release = release.Trim();
    });
}

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Host.UseSerilog((ctx, services, cfg) =>
{
    var loggerConfig = cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

    if (ctx.HostingEnvironment.IsDevelopment())
        loggerConfig.WriteTo.File("logs/floristhub-.log", rollingInterval: RollingInterval.Day);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddFloristHubDal(builder.Configuration);
builder.Services.AddFloristHubServices(builder.Configuration);
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FloristHub API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT no header Authorization: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key em falta (defina Jwt__Key).");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

var rateLimitDisabled = builder.Configuration.GetValue("Features:DisableRateLimiting", false);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
    {
        if (rateLimitDisabled)
            return RateLimitPartition.GetNoLimiter("integration-tests");
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

builder.Services.AddCors();
builder.Services.AddSingleton<TenantCorsOriginChecker>();
builder.Services.AddSingleton<FloristHub.Services.Interfaces.ICorsDomainCacheInvalidator>(
    sp => sp.GetRequiredService<TenantCorsOriginChecker>());

var app = builder.Build();

var corsChecker = app.Services.GetRequiredService<TenantCorsOriginChecker>();
app.UseCors(policy => policy
    .SetIsOriginAllowed(corsChecker.IsAllowed)
    .AllowAnyHeader()
    .WithExposedHeaders("Content-Type", "Retry-After")
    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"));

if (!string.IsNullOrWhiteSpace(sentryDsn))
    app.UseSentryTracing();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseRateLimiter();

if (!app.Environment.IsDevelopment())
    app.UseForwardedHeaders();

app.UseSerilogRequestLogging();
app.UseMiddleware<AcceptLanguageMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();

if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    app.Use(async (context, next) =>
    {
        var tenantContext = context.RequestServices.GetRequiredService<FloristHub.DAL.MultiTenancy.ITenantContext>();
        SentrySdk.ConfigureScope(scope =>
        {
            if (tenantContext.TenantId is Guid tenantId)
                scope.SetTag("tenant", tenantId.ToString("N"));
            else if (!string.IsNullOrWhiteSpace(tenantContext.TenantSlug))
                scope.SetTag("tenant", tenantContext.TenantSlug!);
        });
        await next();
    });
}

app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "floristhub-api" }))
    .AllowAnonymous()
    .ExcludeFromDescription();

if (migrateOnly)
{
    await app.ApplyFloristHubDatabaseAsync(forceMigrate: true);
    return;
}

await app.ApplyFloristHubDatabaseAsync();
app.Logger.LogInformation("Florist Hub API a escutar. Ambiente {Environment}.", app.Environment.EnvironmentName);
app.Run();

/// <summary>Expõe o entrypoint para <c>WebApplicationFactory</c>.</summary>
public partial class Program;
