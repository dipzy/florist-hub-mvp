using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

/// <summary>
/// Comissão global da plataforma e mensalidade prevista (a mensalidade não é cobrada).
/// </summary>
public class PlatformBillingSettings : BaseEntity
{
    public static readonly Guid SingletonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const decimal DefaultCommissionPercent = 10m;

    public decimal CommissionPercent { get; set; } = DefaultCommissionPercent;
    public decimal MonthlyFeeAmount { get; set; }
}
