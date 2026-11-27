namespace FloristHub.Services.Push;

public enum PushAudience
{
    TenantFlorists = 0,
    Customer = 1
}

public enum NotificationPreferenceKind
{
    NewOrder = 0,
    OrderPaid = 1,
    OrderStatus = 2,
    LowStock = 3,
    RecurringSubscriptions = 4
}

public enum PushDeliveryOutcome
{
    Sent = 0,
    InvalidToken = 1,
    Retry = 2,
    Drop = 3
}

public sealed record PushWorkItem(
    Guid TenantId,
    PushAudience Audience,
    Guid? UserId,
    Guid OrderId,
    string Title,
    string Body,
    string Type,
    NotificationPreferenceKind Kind);

public sealed record PushMessage(
    string DeviceToken,
    Guid TenantId,
    Guid OrderId,
    string Title,
    string Body,
    string Type);

public sealed record PushSendResult(PushDeliveryOutcome Outcome);

public interface IPushQueue
{
    void Enqueue(PushWorkItem item);
}

public interface IPushSender
{
    Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default);
}
