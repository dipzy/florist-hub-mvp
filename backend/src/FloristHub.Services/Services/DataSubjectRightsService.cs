using FloristHub.DAL.Audit;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Entities;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Legal;
using FloristHub.Services.Legal;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services;

public interface IDataSubjectRightsService
{
    Task<CustomerDataExportDto> ExportMyDataAsync(Guid userId, CancellationToken ct = default);
    Task AnonymizeMyAccountAsync(Guid userId, CancellationToken ct = default);
}

public class DataSubjectRightsService : IDataSubjectRightsService
{
    private const string AnonymizedEmailDomain = "anon.floristhub.invalid";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;

    public DataSubjectRightsService(
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        ITenantContext tenant,
        IAuditLogger audit)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<CustomerDataExportDto> ExportMyDataAsync(Guid userId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("Conta não encontrada.");

        if (user.IsAnonymized)
            throw new InvalidOperationException("Conta anonimizada.");

        var orders = await _unitOfWork.Orders.QueryWithDetails()
            .Where(o => o.CustomerId == userId && o.TenantId == tenantId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        await _audit.LogAsync(
            ComplianceAuditActions.DataExport,
            userId,
            tenantId,
            nameof(ApplicationUser),
            userId,
            $"orders={orders.Count}",
            ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new CustomerDataExportDto(
            user.Id,
            user.Email,
            user.FullName,
            user.MarketingConsent,
            user.MarketingConsentAt,
            user.AcceptedTermsVersion,
            user.AcceptedTermsAt,
            user.AcceptedPrivacyVersion,
            user.AcceptedPrivacyAt,
            orders.Select(o => new CustomerOrderExportDto(
                o.Id,
                o.CreatedAt,
                (int)o.Status,
                o.EstimatedTotal,
                o.DeliveryFee,
                o.DeliveryAddress,
                o.CustomerNotes,
                o.Items.Select(i => new CustomerOrderItemExportDto(i.Name, i.Quantity, i.UnitPrice)).ToList()))
                .ToList(),
            DateTimeOffset.UtcNow);
    }

    public async Task AnonymizeMyAccountAsync(Guid userId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("Conta não encontrada.");

        if (user.IsAnonymized)
            throw new InvalidOperationException("Conta já anonimizada.");

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(Roles.Customer))
            throw new InvalidOperationException("Apenas contas de cliente podem ser anonimizadas por este fluxo.");

        var orders = await _unitOfWork.Orders.Query()
            .Where(o => o.CustomerId == userId && o.TenantId == tenantId)
            .ToListAsync(ct);

        foreach (var order in orders)
        {
            order.DeliveryAddress = null;
            order.CustomerTaxId = null;
            if (!string.IsNullOrWhiteSpace(order.CustomerNotes))
                order.CustomerNotes = "[anonimizado]";
            order.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var reviews = await _unitOfWork.Reviews.Query().Where(r => r.CustomerId == userId).ToListAsync(ct);
        foreach (var review in reviews)
        {
            review.Comment = null;
            review.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var anonEmail = $"anonymized-{user.Id:N}@{AnonymizedEmailDomain}";
        user.FullName = "Utilizador anonimizado";
        user.Email = anonEmail;
        user.UserName = anonEmail;
        user.NormalizedEmail = anonEmail.ToUpperInvariant();
        user.NormalizedUserName = anonEmail.ToUpperInvariant();
        user.PhoneNumber = null;
        user.MarketingConsent = false;
        user.MarketingConsentAt = null;
        user.IsAnonymized = true;
        user.AnonymizedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _audit.LogAsync(
            ComplianceAuditActions.AccountAnonymized,
            userId,
            tenantId,
            nameof(ApplicationUser),
            userId,
            $"orders={orders.Count}",
            ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");
}
