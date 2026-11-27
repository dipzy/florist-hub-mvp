using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Push;

public sealed record NotificationPreferencesDto(
    bool NewOrder,
    bool OrderPaid,
    bool OrderStatus,
    bool LowStock = true,
    bool RecurringSubscriptions = true);

public sealed record UpdateNotificationPreferencesRequest(
    bool NewOrder,
    bool OrderPaid,
    bool OrderStatus,
    bool LowStock = true,
    bool RecurringSubscriptions = true);

public interface IPushDeviceService
{
    Task RegisterAsync(Guid userId, string token, string platform, CancellationToken cancellationToken = default);
    Task UnregisterAsync(Guid userId, string token, CancellationToken cancellationToken = default);
    Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken = default);
}

public sealed class PushDeviceService : IPushDeviceService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public PushDeviceService(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task RegisterAsync(Guid userId, string token, string platform, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var normalized = NormalizeToken(token);
        var parsed = ParsePlatform(platform);
        var now = DateTimeOffset.UtcNow;

        var existing = await _db.PushDevices
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Token == normalized, cancellationToken);

        if (existing is null)
        {
            _db.PushDevices.Add(new PushDevice
            {
                TenantId = tenantId,
                UserId = userId,
                Token = normalized,
                Platform = parsed,
                LastSeenAt = now
            });
        }
        else
        {
            existing.TenantId = tenantId;
            existing.UserId = userId;
            existing.Platform = parsed;
            existing.LastSeenAt = now;
            existing.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnregisterAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var normalized = NormalizeToken(token);
        var device = await _db.PushDevices
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.UserId == userId && d.Token == normalized, cancellationToken)
            ?? throw new KeyNotFoundException("Dispositivo não encontrado nesta loja.");

        _db.PushDevices.Remove(device);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var preference = await _db.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId, cancellationToken);
        return Map(preference);
    }

    public async Task<NotificationPreferencesDto> UpdatePreferencesAsync(
        Guid userId,
        UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var preference = await _db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId, cancellationToken);

        if (preference is null)
        {
            preference = new NotificationPreference { TenantId = tenantId, UserId = userId };
            _db.NotificationPreferences.Add(preference);
        }

        preference.NewOrder = request.NewOrder;
        preference.OrderPaid = request.OrderPaid;
        preference.OrderStatus = request.OrderStatus;
        preference.LowStock = request.LowStock;
        preference.RecurringSubscriptions = request.RecurringSubscriptions;
        preference.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(preference);
    }

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new UnauthorizedAccessException("Loja não resolvida.");

    private static string NormalizeToken(string token)
    {
        var value = token.Trim();
        if (value.Length is < 16 or > 512 || value.Any(char.IsWhiteSpace))
            throw new InvalidOperationException("Token do dispositivo inválido.");
        return value;
    }

    private static PushPlatform ParsePlatform(string platform)
    {
        if (platform.Equals("Android", StringComparison.OrdinalIgnoreCase))
            return PushPlatform.Android;
        if (platform.Equals("Ios", StringComparison.OrdinalIgnoreCase) || platform.Equals("iOS", StringComparison.OrdinalIgnoreCase))
            return PushPlatform.Ios;
        throw new InvalidOperationException("Plataforma inválida. Use Android ou Ios.");
    }

    private static NotificationPreferencesDto Map(NotificationPreference? preference) =>
        preference is null
            ? new NotificationPreferencesDto(true, true, true, true, true)
            : new NotificationPreferencesDto(preference.NewOrder, preference.OrderPaid, preference.OrderStatus, preference.LowStock, preference.RecurringSubscriptions);
}
