using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Marketing;

public interface ICouponService
{
    Task<IReadOnlyList<CouponDto>> ListAsync(CancellationToken ct = default);
    Task<CouponDto> CreateAsync(SaveCouponRequest request, Guid userId, CancellationToken ct = default);
    Task<CouponDto> UpdateAsync(Guid id, SaveCouponRequest request, Guid userId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}

public sealed class CouponService : ICouponService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;

    public CouponService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CouponDto>> ListAsync(CancellationToken ct = default)
    {
        var coupons = await _unitOfWork.Coupons.Query().Include(c => c.Targets).OrderBy(c => c.Code).ToListAsync(ct);
        var counts = await _unitOfWork.CouponRedemptions.Query()
            .Where(r => !r.IsVoided)
            .GroupBy(r => r.CouponId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return coupons.Select(c => Map(c, counts.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<CouponDto> CreateAsync(SaveCouponRequest request, Guid userId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var coupon = new Coupon { TenantId = tenantId, Code = "TMP" };
        await ApplyAsync(coupon, request, ct);
        if (await _unitOfWork.Coupons.Query().AnyAsync(c => c.Code == coupon.Code, ct))
            throw new InvalidOperationException("Já existe um cupão com este código.");
        await _unitOfWork.Coupons.AddAsync(coupon, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("CouponCreated", userId, tenantId, nameof(Coupon), coupon.Id, coupon.Code, ct);
        return Map(coupon, 0);
    }

    public async Task<CouponDto> UpdateAsync(Guid id, SaveCouponRequest request, Guid userId, CancellationToken ct = default)
    {
        var coupon = await _unitOfWork.Coupons.Query().Include(c => c.Targets).FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException("Cupão não encontrado.");
        var previous = coupon.Code;
        await ApplyAsync(coupon, request, ct);
        if (await _unitOfWork.Coupons.Query().AnyAsync(c => c.Id != coupon.Id && c.Code == coupon.Code, ct))
            throw new InvalidOperationException("Já existe um cupão com este código.");
        coupon.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("CouponUpdated", userId, coupon.TenantId, nameof(Coupon), coupon.Id, previous + "->" + coupon.Code, ct);
        var count = await _unitOfWork.CouponRedemptions.Query().CountAsync(r => r.CouponId == coupon.Id && !r.IsVoided, ct);
        return Map(coupon, count);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var coupon = await _unitOfWork.Coupons.Query().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException("Cupão não encontrado.");
        var used = await _unitOfWork.CouponRedemptions.Query().AnyAsync(r => r.CouponId == coupon.Id, ct);
        if (used)
        {
            coupon.IsActive = false;
            coupon.UpdatedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            await _audit.LogAsync("CouponDeactivated", userId, coupon.TenantId, nameof(Coupon), coupon.Id, coupon.Code, ct);
            return;
        }

        _unitOfWork.Coupons.Remove(coupon);
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("CouponDeleted", userId, coupon.TenantId, nameof(Coupon), coupon.Id, coupon.Code, ct);
    }

    private async Task ApplyAsync(Coupon coupon, SaveCouponRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.DiscountType))
            throw new InvalidOperationException("Tipo de desconto inválido.");
        coupon.Code = CouponMath.NormalizeCode(request.Code);
        CouponMath.EnsureDiscountValue(request.DiscountType, request.DiscountValue);
        if (request.MinimumOrderAmount is decimal minimum)
            CouponMath.EnsureMoney(minimum, "O valor mínimo");
        if (request.ValidFrom is DateTimeOffset from && request.ValidUntil is DateTimeOffset until && until < from)
            throw new InvalidOperationException("A data de fim tem de ser posterior ao início.");
        if (request.MaxRedemptions is < 1 || request.MaxRedemptionsPerCustomer is < 1)
            throw new InvalidOperationException("O limite de utilizações tem de ser pelo menos 1.");

        coupon.DiscountType = request.DiscountType;
        coupon.DiscountValue = request.DiscountValue;
        coupon.ValidFrom = request.ValidFrom;
        coupon.ValidUntil = request.ValidUntil;
        coupon.MaxRedemptions = request.MaxRedemptions;
        coupon.MaxRedemptionsPerCustomer = request.MaxRedemptionsPerCustomer;
        coupon.MinimumOrderAmount = request.MinimumOrderAmount;
        coupon.IsActive = request.IsActive;
        coupon.Targets.Clear();
        foreach (var target in request.Targets ?? [])
        {
            if (target.FlowerId is null && string.IsNullOrWhiteSpace(target.Category))
                continue;
            if (target.FlowerId is not null && !string.IsNullOrWhiteSpace(target.Category))
                throw new InvalidOperationException("Cada abrangência do cupão é uma flor ou uma categoria.");
            if (target.FlowerId is Guid flowerId)
            {
                var exists = await _unitOfWork.Flowers.Query().AnyAsync(f => f.Id == flowerId, ct);
                if (!exists)
                    throw new InvalidOperationException("Uma das flores do cupão não existe nesta loja.");
            }

            coupon.Targets.Add(new CouponTarget
            {
                TenantId = coupon.TenantId,
                FlowerId = target.FlowerId,
                Category = string.IsNullOrWhiteSpace(target.Category) ? null : CouponMath.NormalizeCategory(target.Category)
            });
        }
    }

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");

    private static CouponDto Map(Coupon coupon, int redemptions) =>
        new(
            coupon.Id,
            coupon.Code,
            coupon.DiscountType,
            coupon.DiscountValue,
            coupon.ValidFrom,
            coupon.ValidUntil,
            coupon.MaxRedemptions,
            coupon.MaxRedemptionsPerCustomer,
            coupon.MinimumOrderAmount,
            coupon.IsActive,
            redemptions,
            coupon.Targets.Select(t => new CouponTargetInput(t.FlowerId, t.Category)).ToList());
}
