namespace FloristHub.DAL.Common;

/// <summary>
/// Marca entidades cujos dados pertencem a um tenant (florista).
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
