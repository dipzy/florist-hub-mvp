using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

/// <summary>Convite de florista com validade e uso único (reenvios revogam convites anteriores).</summary>
public class FloristInvite : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
