using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

/// <summary>Notificação in-app no painel da florista (badge e lista).</summary>
public class FloristPanelNotification : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public FloristPanelNotificationKind Kind { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
