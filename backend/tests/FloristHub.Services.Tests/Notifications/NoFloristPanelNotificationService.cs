using FloristHub.Services.Notifications;

namespace FloristHub.Services.Tests.Notifications;

internal sealed class NoFloristPanelNotificationService : IFloristPanelNotificationService
{
    public Task RecordNewOrderAsync(Guid tenantId, Guid orderId, CancellationToken ct = default) => Task.CompletedTask;

    public Task RecordPaymentReceivedAsync(Guid tenantId, Guid orderId, CancellationToken ct = default) => Task.CompletedTask;

    public Task<FloristPanelNotificationSummaryDto> GetSummaryAsync(CancellationToken ct = default) =>
        Task.FromResult(new FloristPanelNotificationSummaryDto(0));

    public Task<IReadOnlyList<FloristPanelNotificationDto>> ListUnreadAsync(int limit, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<FloristPanelNotificationDto>>([]);

    public Task MarkAllReadAsync(CancellationToken ct = default) => Task.CompletedTask;
}
