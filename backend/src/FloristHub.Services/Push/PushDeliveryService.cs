using FloristHub.DAL.Audit;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Persistence;
using FloristHub.Services.Push;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Push;

public sealed class PushDeliveryService
{
    private readonly ApplicationDbContext _db;
    private readonly IPushSender _sender;
    private readonly PushOptions _options;
    private readonly IAuditLogger _audit;
    private readonly ILogger<PushDeliveryService> _logger;

    public PushDeliveryService(
        ApplicationDbContext db,
        IPushSender sender,
        IOptions<PushOptions> options,
        IAuditLogger audit,
        ILogger<PushDeliveryService> logger)
    {
        _db = db;
        _sender = sender;
        _options = options.Value;
        _audit = audit;
        _logger = logger;
    }

    public async Task DeliverAsync(PushWorkItem item, CancellationToken cancellationToken = default)
    {
        var userIds = await ResolveUsersAsync(item, cancellationToken);
        foreach (var userId in userIds)
        {
            if (!await IsEnabledAsync(item, userId, cancellationToken))
                continue;

            var devices = await _db.PushDevices
                .IgnoreQueryFilters()
                .Where(d => d.TenantId == item.TenantId && d.UserId == userId)
                .ToListAsync(cancellationToken);

            foreach (var device in devices)
                await SendOneAsync(item, device, cancellationToken);
        }
    }

    private async Task SendOneAsync(PushWorkItem item, PushDevice device, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, _options.MaxSendAttempts);
        PushSendResult? last = null;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            last = await _sender.SendAsync(
                new PushMessage(device.Token, item.TenantId, item.OrderId, item.Title, item.Body, item.Type),
                cancellationToken);

            if (last.Outcome == PushDeliveryOutcome.Sent)
                return;

            if (last.Outcome == PushDeliveryOutcome.InvalidToken)
            {
                _db.PushDevices.Remove(device);
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Token push inválido removido Device={DeviceId} Order={OrderId}", device.Id, item.OrderId);
                return;
            }

            if (last.Outcome == PushDeliveryOutcome.Drop || attempt == attempts)
                break;

            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), cancellationToken);
        }

        _logger.LogError(
            "Push não entregue Order={OrderId} Tenant={TenantId} Outcome={Outcome}",
            item.OrderId,
            item.TenantId,
            last?.Outcome);
        try
        {
            await _audit.LogAsync(
                "PushDeliveryFailed",
                null,
                item.TenantId,
                "Push",
                item.OrderId,
                $"{item.Type}; {last?.Outcome}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível registar auditoria de falha de push");
        }
    }

    private async Task<List<Guid>> ResolveUsersAsync(PushWorkItem item, CancellationToken cancellationToken)
    {
        if (item.Audience == PushAudience.Customer)
            return item.UserId is null ? [] : [item.UserId.Value];

        var fromMembership = await (
            from membership in _db.FloristTenantMemberships.AsNoTracking()
            join userRole in _db.UserRoles.AsNoTracking() on membership.UserId equals userRole.UserId
            join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where membership.TenantId == item.TenantId && role.Name == Roles.Florist
            select membership.UserId).ToListAsync(cancellationToken);

        var fromLegacyTenant = await (
            from user in _db.Users.AsNoTracking()
            join userRole in _db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where user.TenantId == item.TenantId && role.Name == Roles.Florist
            select user.Id).ToListAsync(cancellationToken);

        return fromMembership.Concat(fromLegacyTenant).Distinct().ToList();
    }

    private async Task<bool> IsEnabledAsync(PushWorkItem item, Guid userId, CancellationToken cancellationToken)
    {
        var preference = await _db.NotificationPreferences
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == item.TenantId && p.UserId == userId, cancellationToken);

        if (preference is null)
            return true;

        return item.Kind switch
        {
            NotificationPreferenceKind.NewOrder => preference.NewOrder,
            NotificationPreferenceKind.OrderPaid => preference.OrderPaid,
            NotificationPreferenceKind.OrderStatus => preference.OrderStatus,
            NotificationPreferenceKind.LowStock => preference.LowStock,
            NotificationPreferenceKind.RecurringSubscriptions => preference.RecurringSubscriptions,
            _ => false
        };
    }
}
