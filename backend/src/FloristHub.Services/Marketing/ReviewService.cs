using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Marketing;

public interface IReviewService
{
    Task<OrderReviewDto> CreateAsync(Guid orderId, Guid customerId, CreateReviewRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<OrderReviewDto>> ListForFloristAsync(CancellationToken ct = default);
    Task<OrderReviewDto> ReplyAsync(Guid reviewId, string reply, Guid floristId, CancellationToken ct = default);
    Task<OrderReviewDto> SetHiddenAsync(Guid reviewId, bool hidden, string? reason, Guid floristId, CancellationToken ct = default);
    Task<OrderReviewDto> ApproveAsync(Guid reviewId, Guid floristId, CancellationToken ct = default);
    Task<IReadOnlyList<PublicReviewDto>> ListPublicAsync(CancellationToken ct = default);
    Task<RatingSummaryDto> SummaryAsync(CancellationToken ct = default);
}

public sealed class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;

    public ReviewService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<OrderReviewDto> CreateAsync(Guid orderId, Guid customerId, CreateReviewRequest request, CancellationToken ct = default)
    {
        if (request.Stars is < 1 or > 5)
            throw new InvalidOperationException("A avaliação tem de ter entre 1 e 5 estrelas.");
        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        if (comment is { Length: > 2000 })
            throw new InvalidOperationException("O comentário pode ter no máximo 2000 caracteres.");

        var order = await _unitOfWork.Orders.Query().FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new KeyNotFoundException("Encomenda não encontrada.");
        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException("Só pode avaliar as suas encomendas.");
        if (order.Status != OrderStatus.Delivered)
            throw new InvalidOperationException("Só pode avaliar uma encomenda depois de entregue.");
        if (await _unitOfWork.Reviews.Query().AnyAsync(r => r.OrderId == order.Id, ct))
            throw new InvalidOperationException("Esta encomenda já foi avaliada.");

        var review = new OrderReview
        {
            TenantId = order.TenantId,
            OrderId = order.Id,
            CustomerId = customerId,
            Stars = request.Stars,
            Comment = comment,
            IsApproved = false,
        };
        await _unitOfWork.Reviews.AddAsync(review, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("ReviewCreated", customerId, order.TenantId, nameof(OrderReview), review.Id, request.Stars.ToString(), ct);
        return Map(review, null);
    }

    public async Task<IReadOnlyList<OrderReviewDto>> ListForFloristAsync(CancellationToken ct = default)
    {
        var rows = await _unitOfWork.Reviews.Query()
            .Include(r => r.Order)!.ThenInclude(o => o!.Customer)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(r => Map(r, r.Order?.Customer?.FullName)).ToList();
    }

    public async Task<OrderReviewDto> ReplyAsync(Guid reviewId, string reply, Guid floristId, CancellationToken ct = default)
    {
        var text = (reply ?? "").Trim();
        if (text.Length is < 1 or > 2000)
            throw new InvalidOperationException("A resposta tem de ter entre 1 e 2000 caracteres.");
        var review = await LoadAsync(reviewId, ct);
        review.FloristReply = text;
        review.FloristRepliedAt = DateTimeOffset.UtcNow;
        review.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("ReviewReplied", floristId, review.TenantId, nameof(OrderReview), review.Id, null, ct);
        return Map(review, review.Order?.Customer?.FullName);
    }

    public async Task<OrderReviewDto> SetHiddenAsync(Guid reviewId, bool hidden, string? reason, Guid floristId, CancellationToken ct = default)
    {
        var review = await LoadAsync(reviewId, ct);
        review.IsHidden = hidden;
        review.HiddenReason = hidden ? TrimReason(reason) : null;
        review.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync(hidden ? "ReviewHidden" : "ReviewRestored", floristId, review.TenantId, nameof(OrderReview), review.Id, review.HiddenReason, ct);
        return Map(review, review.Order?.Customer?.FullName);
    }

    public async Task<OrderReviewDto> ApproveAsync(Guid reviewId, Guid floristId, CancellationToken ct = default)
    {
        var review = await LoadAsync(reviewId, ct);
        if (review.IsHidden)
            throw new InvalidOperationException("Não pode aprovar uma avaliação oculta.");
        review.IsApproved = true;
        review.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("ReviewApproved", floristId, review.TenantId, nameof(OrderReview), review.Id, null, ct);
        return Map(review, review.Order?.Customer?.FullName);
    }

    public async Task<IReadOnlyList<PublicReviewDto>> ListPublicAsync(CancellationToken ct = default)
    {
        await TenantPublicAccessRules.RequirePublishedTenantAsync(_tenant, _unitOfWork, ct);
        var rows = await _unitOfWork.Reviews.Query()
            .Where(r => r.IsApproved && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .Take(30)
            .ToListAsync(ct);
        return rows.Select(r => new PublicReviewDto(r.Stars, r.Comment, r.FloristReply, r.CreatedAt)).ToList();
    }

    public async Task<RatingSummaryDto> SummaryAsync(CancellationToken ct = default)
    {
        await TenantPublicAccessRules.RequirePublishedTenantAsync(_tenant, _unitOfWork, ct);
        var query = _unitOfWork.Reviews.Query().Where(r => r.IsApproved && !r.IsHidden);
        var count = await query.CountAsync(ct);
        if (count == 0)
            return new RatingSummaryDto(0, 0);
        var average = await query.AverageAsync(r => (decimal)r.Stars, ct);
        return new RatingSummaryDto(decimal.Round(average, 2, MidpointRounding.AwayFromZero), count);
    }

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Loja não resolvida.");

    private async Task<OrderReview> LoadAsync(Guid id, CancellationToken ct) =>
        await _unitOfWork.Reviews.Query().Include(r => r.Order)!.ThenInclude(o => o!.Customer).FirstOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new KeyNotFoundException("Avaliação não encontrada.");

    private static string? TrimReason(string? reason)
    {
        var value = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (value is { Length: > 500 })
            throw new InvalidOperationException("O motivo é demasiado longo.");
        return value;
    }

    private static OrderReviewDto Map(OrderReview review, string? customerName) =>
        new(review.Id, review.OrderId, review.Stars, review.Comment, review.FloristReply, review.FloristRepliedAt, review.IsHidden, review.IsApproved, customerName, review.CreatedAt);
}
