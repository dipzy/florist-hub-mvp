using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Marketing;

public static class OrderPricing
{
    public static async Task ApplySpecialPricesAsync(IUnitOfWork unitOfWork, Order order, CancellationToken ct)
    {
        var flowerIds = order.Items.Where(i => i.FlowerId.HasValue).Select(i => i.FlowerId!.Value).Distinct().ToList();
        if (flowerIds.Count == 0)
            return;

        var today = LisbonClock.Today();
        var offers = await unitOfWork.CampaignFlowers.Query()
            .Where(f => flowerIds.Contains(f.FlowerId) && f.SpecialPrice != null)
            .Where(f => f.Campaign!.IsActive && f.Campaign.StartsOn <= today && f.Campaign.EndsOn >= today)
            .Select(f => new { f.FlowerId, Price = f.SpecialPrice!.Value })
            .ToListAsync(ct);

        if (offers.Count == 0)
            return;

        var best = offers.GroupBy(o => o.FlowerId).ToDictionary(g => g.Key, g => g.Min(x => x.Price));
        var flowers = await unitOfWork.Flowers.Query()
            .Where(f => flowerIds.Contains(f.Id))
            .Select(f => new { f.Id, f.Name, f.StockQuantity })
            .ToDictionaryAsync(f => f.Id, ct);
        foreach (var item in order.Items)
        {
            if (item.FlowerId is not Guid flowerId || !best.TryGetValue(flowerId, out var price))
                continue;
            if (flowers.TryGetValue(flowerId, out var flower) && flower.StockQuantity is <= 0)
                throw new InvalidOperationException($"{flower.Name} está esgotada e não pode ser comprada nesta campanha.");
            item.UnitPrice = price;
        }
    }

    public static async Task CopyCategoriesAsync(IUnitOfWork unitOfWork, Order order, CancellationToken ct)
    {
        var flowerIds = order.Items.Where(i => i.FlowerId.HasValue).Select(i => i.FlowerId!.Value).Distinct().ToList();
        if (flowerIds.Count == 0)
            return;
        var categories = await unitOfWork.Flowers.Query()
            .Where(f => flowerIds.Contains(f.Id))
            .Select(f => new { f.Id, f.Category })
            .ToDictionaryAsync(f => f.Id, f => f.Category, ct);
        foreach (var item in order.Items)
        {
            if (item.FlowerId is Guid flowerId && categories.TryGetValue(flowerId, out var category))
                item.Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        }
    }

    public static void RefreshPayable(Order order, decimal discount)
    {
        var merchandise = Merchandise(order);
        order.DiscountAmount = discount;
        order.EstimatedTotal = CouponMath.Payable(merchandise, order.DeliveryFee, discount);
    }

    public static decimal Merchandise(Order order) =>
        CouponMath.Round(order.Items.Sum(i => CouponMath.LineGross(i.UnitPrice, i.Quantity)));

    public static async Task ApplyCouponAsync(IUnitOfWork unitOfWork, Order order, string code, Guid customerId, CancellationToken ct)
    {
        await EnsureCouponMutableAsync(unitOfWork, order, ct);
        await CopyCategoriesAsync(unitOfWork, order, ct);
        var normalized = CouponMath.NormalizeCode(code);
        var coupon = await unitOfWork.Coupons.Query()
            .Include(c => c.Targets)
            .FirstOrDefaultAsync(c => c.Code == normalized, ct)
            ?? throw new InvalidOperationException("Cupão inválido.");

        await unitOfWork.LockCouponAsync(order.TenantId, coupon.Id, ct);
        await EnsureFlowersInStockAsync(unitOfWork, order, "com este cupão", ct);
        var discount = await QuoteAsync(unitOfWork, coupon, order, customerId, ct);
        await CancelPendingPaymentsAsync(unitOfWork, order, ct);
        await VoidRedemptionsAsync(unitOfWork, order.Id, ct);

        order.CouponId = coupon.Id;
        order.CouponCode = coupon.Code;
        RefreshPayable(order, discount);
        await unitOfWork.CouponRedemptions.AddAsync(new CouponRedemption
        {
            TenantId = order.TenantId,
            CouponId = coupon.Id,
            OrderId = order.Id,
            CustomerId = customerId,
            DiscountAmount = discount
        }, ct);
    }

    public static async Task RemoveCouponAsync(IUnitOfWork unitOfWork, Order order, CancellationToken ct)
    {
        await EnsureCouponMutableAsync(unitOfWork, order, ct);
        await CancelPendingPaymentsAsync(unitOfWork, order, ct);
        await VoidRedemptionsAsync(unitOfWork, order.Id, ct);
        order.CouponId = null;
        order.CouponCode = null;
        RefreshPayable(order, 0);
    }

    public static async Task ReapplyOrClearAsync(IUnitOfWork unitOfWork, Order order, bool clearBecauseRevisedTotal, CancellationToken ct)
    {
        if (clearBecauseRevisedTotal || order.CouponId is null)
        {
            if (order.CouponId is not null || order.DiscountAmount != 0)
            {
                await VoidRedemptionsAsync(unitOfWork, order.Id, ct);
                order.CouponId = null;
                order.CouponCode = null;
                order.DiscountAmount = 0;
            }

            return;
        }

        var coupon = await unitOfWork.Coupons.Query().Include(c => c.Targets).FirstOrDefaultAsync(c => c.Id == order.CouponId, ct);
        if (coupon is not null)
            await unitOfWork.LockCouponAsync(order.TenantId, coupon.Id, ct);
        if (coupon is null)
        {
            await VoidRedemptionsAsync(unitOfWork, order.Id, ct);
            order.CouponId = null;
            order.CouponCode = null;
            RefreshPayable(order, 0);
            return;
        }

        await CopyCategoriesAsync(unitOfWork, order, ct);
        await EnsureFlowersInStockAsync(unitOfWork, order, "com este cupão", ct);
        var discount = await QuoteAsync(unitOfWork, coupon, order, order.CustomerId, ct);
        var redemption = await unitOfWork.CouponRedemptions.Query()
            .FirstOrDefaultAsync(r => r.OrderId == order.Id && r.CouponId == coupon.Id && !r.IsVoided, ct);
        if (redemption is null)
        {
            await unitOfWork.CouponRedemptions.AddAsync(new CouponRedemption
            {
                TenantId = order.TenantId,
                CouponId = coupon.Id,
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                DiscountAmount = discount
            }, ct);
        }
        else
        {
            redemption.DiscountAmount = discount;
            redemption.UpdatedAt = DateTimeOffset.UtcNow;
        }

        order.CouponCode = coupon.Code;
        RefreshPayable(order, discount);
    }

    public static async Task VoidCouponAsync(IUnitOfWork unitOfWork, Guid orderId, CancellationToken ct) =>
        await VoidRedemptionsAsync(unitOfWork, orderId, ct);

    private static async Task<decimal> QuoteAsync(IUnitOfWork unitOfWork, Coupon coupon, Order order, Guid customerId, CancellationToken ct)
    {
        if (!coupon.IsActive)
            throw new InvalidOperationException("Este cupão já não está ativo.");
        var now = DateTimeOffset.UtcNow;
        if (coupon.ValidFrom is DateTimeOffset from && now < from)
            throw new InvalidOperationException("Este cupão ainda não é válido.");
        if (coupon.ValidUntil is DateTimeOffset until && now > until)
            throw new InvalidOperationException("Este cupão expirou.");

        var merchandise = Merchandise(order);
        var beforeDiscount = CouponMath.Payable(merchandise, order.DeliveryFee, 0);
        if (coupon.MinimumOrderAmount is decimal minimum && beforeDiscount < minimum)
            throw new InvalidOperationException($"O cupão exige uma encomenda de pelo menos {CouponMath.FormatEuro(minimum)}.");

        var used = await unitOfWork.CouponRedemptions.Query()
            .CountAsync(r => r.CouponId == coupon.Id && !r.IsVoided && r.OrderId != order.Id, ct);
        if (coupon.MaxRedemptions is int max && used >= max)
            throw new InvalidOperationException("Este cupão já atingiu o número máximo de utilizações.");

        var usedByCustomer = await unitOfWork.CouponRedemptions.Query()
            .CountAsync(r => r.CouponId == coupon.Id && r.CustomerId == customerId && !r.IsVoided && r.OrderId != order.Id, ct);
        if (coupon.MaxRedemptionsPerCustomer is int perCustomer && usedByCustomer >= perCustomer)
            throw new InvalidOperationException("Já utilizou este cupão o número máximo de vezes.");

        var restricted = coupon.Targets.Count > 0;
        var flowerIds = coupon.Targets.Where(t => t.FlowerId.HasValue).Select(t => t.FlowerId!.Value).ToHashSet();
        var categories = coupon.Targets
            .Where(t => !string.IsNullOrWhiteSpace(t.Category))
            .Select(t => t.Category!.Trim().ToLowerInvariant())
            .ToHashSet();
        var lines = order.Items
            .Select(i => new CouponLine(i.FlowerId, i.Category, CouponMath.LineGross(i.UnitPrice, i.Quantity)))
            .ToList();
        var eligible = CouponMath.Eligible(lines, restricted, flowerIds, categories);
        if (restricted && eligible <= 0)
            throw new InvalidOperationException("O cupão não abrange os artigos desta encomenda.");

        var discount = CouponMath.Discount(coupon.DiscountType, coupon.DiscountValue, eligible);
        if (discount <= 0)
            throw new InvalidOperationException("O desconto deste cupão é inferior a um cêntimo.");
        return discount;
    }

    private static async Task EnsureFlowersInStockAsync(IUnitOfWork unitOfWork, Order order, string channel, CancellationToken ct)
    {
        var flowerIds = order.Items.Where(i => i.FlowerId.HasValue).Select(i => i.FlowerId!.Value).Distinct().ToList();
        if (flowerIds.Count == 0)
            return;
        var exhausted = await unitOfWork.Flowers.Query()
            .Where(f => flowerIds.Contains(f.Id) && f.StockQuantity != null && f.StockQuantity <= 0)
            .Select(f => f.Name)
            .FirstOrDefaultAsync(ct);
        if (exhausted is not null)
            throw new InvalidOperationException($"{exhausted} está esgotada e não pode ser comprada {channel}.");
    }

    private static async Task EnsureCouponMutableAsync(IUnitOfWork unitOfWork, Order order, CancellationToken ct)
    {
        if (order.PaymentCaptured || order.Status is OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.Delivered)
            throw new InvalidOperationException("Já não é possível alterar o cupão desta encomenda.");
        var settled = await unitOfWork.Payments.Query().AnyAsync(
            p => p.OrderId == order.Id && (p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.PartiallyRefunded || p.Status == PaymentStatus.Refunded),
            ct);
        if (settled)
            throw new InvalidOperationException("Já não é possível alterar o cupão desta encomenda.");
    }

    private static async Task CancelPendingPaymentsAsync(IUnitOfWork unitOfWork, Order order, CancellationToken ct)
    {
        var pending = await unitOfWork.Payments.Query()
            .Where(p => p.OrderId == order.Id && p.Status == PaymentStatus.Pending)
            .ToListAsync(ct);
        foreach (var payment in pending)
        {
            payment.Status = PaymentStatus.Cancelled;
            payment.CheckoutUrl = null;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
        }

        if (pending.Count > 0)
            order.PaymentReference = null;
    }

    private static async Task VoidRedemptionsAsync(IUnitOfWork unitOfWork, Guid orderId, CancellationToken ct)
    {
        var rows = await unitOfWork.CouponRedemptions.Query()
            .Where(r => r.OrderId == orderId && !r.IsVoided)
            .ToListAsync(ct);
        foreach (var row in rows)
        {
            row.IsVoided = true;
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
