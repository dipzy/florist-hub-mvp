using FloristHub.DAL.Audit;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Entities;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Platform;
using FloristHub.Services.Email;
using FloristHub.Services.Interfaces;
using FloristHub.Services.Onboarding;
using FloristHub.Services.Auth;
using FloristHub.Services.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace FloristHub.Services;

public interface IPlatformTenantService
{
    Task<IReadOnlyList<PlatformTenantListItemDto>> ListAsync(CancellationToken ct = default);
    Task<PlatformTenantDetailDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<CreatePlatformTenantResponse> CreateAsync(CreatePlatformTenantRequest request, Guid superAdminUserId, CancellationToken ct = default);
    Task<PlatformTenantDetailDto> UpdateAsync(Guid id, UpdatePlatformTenantRequest request, Guid superAdminUserId, CancellationToken ct = default);
    Task<PlatformTenantDetailDto> SetSuspendedAsync(Guid id, bool suspended, Guid superAdminUserId, CancellationToken ct = default);
    Task<ResendFloristInviteResponse> ResendFloristInviteAsync(Guid tenantId, Guid superAdminUserId, CancellationToken ct = default);
}

public class PlatformTenantService : IPlatformTenantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole<Guid>> _roles;
    private readonly IAuditLogger _audit;
    private readonly IFloristInviteService _invites;
    private readonly ICorsDomainCacheInvalidator _corsCache;
    private readonly IFloristMembershipService _memberships;

    public PlatformTenantService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole<Guid>> roles,
        IAuditLogger audit,
        IFloristInviteService invites,
        ICorsDomainCacheInvalidator corsCache,
        IFloristMembershipService memberships)
    {
        _unitOfWork = unitOfWork;
        _users = users;
        _roles = roles;
        _audit = audit;
        _invites = invites;
        _corsCache = corsCache;
        _memberships = memberships;
    }

    public async Task<IReadOnlyList<PlatformTenantListItemDto>> ListAsync(CancellationToken ct = default)
    {
        var tenants = await _unitOfWork.Tenants.ListAllReadOnlyAsync(ct);
        var orderCounts = await _unitOfWork.Tenants.CountOrdersByTenantAsync(ct);
        return tenants.Select(t => new PlatformTenantListItemDto(
            t.Id,
            t.Name,
            t.Slug,
            t.CustomDomain,
            t.IsActive,
            t.IsPublished,
            orderCounts.GetValueOrDefault(t.Id),
            t.CreatedAt)).ToList();
    }

    public async Task<PlatformTenantDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdReadOnlyAsync(id, ct)
            ?? throw new KeyNotFoundException("Loja não encontrada.");
        var orderCounts = await _unitOfWork.Tenants.CountOrdersByTenantAsync(ct);
        return MapDetail(tenant, orderCounts.GetValueOrDefault(tenant.Id));
    }

    public async Task<CreatePlatformTenantResponse> CreateAsync(
        CreatePlatformTenantRequest request,
        Guid superAdminUserId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("O nome da loja é obrigatório.");

        var slug = TenantSlugRules.NormalizeSlug(request.Slug);
        TenantSlugRules.EnsureValidSlug(slug);

        if (await _unitOfWork.Tenants.SlugExistsAsync(slug, ct))
            throw new InvalidOperationException("Já existe uma loja com este slug.");

        var customDomain = TenantHostResolution.NormalizeCustomDomain(request.CustomDomain);
        if (customDomain is not null && await _unitOfWork.Tenants.CustomDomainExistsAsync(customDomain, null, ct))
            throw new InvalidOperationException("Este domínio próprio já está associado a outra loja.");

        var floristEmail = request.FloristEmail.Trim();
        var existingUser = await _users.FindByEmailAsync(floristEmail);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            CustomDomain = customDomain,
            IsActive = true,
            IsPublished = false,
            OnboardingWizardStep = FloristOnboardingSteps.DefaultFirst,
            ThemePrimaryColor = "#2d6a4f",
            ThemeAccentColor = "#95d5b2",
        };
        ApplyDefaultStoreSettings(tenant);
        await _unitOfWork.Tenants.AddAsync(tenant, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        _corsCache.Invalidate();

        await EnsureRoleAsync(Roles.Florist, ct);

        string? devInviteLink = null;
        var floristInviteSent = false;

        if (existingUser is not null)
        {
            if (await _memberships.HasMembershipAsync(existingUser.Id, tenant.Id, ct))
                throw new InvalidOperationException("Este utilizador já está associado a esta loja.");

            if (!await _users.IsInRoleAsync(existingUser, Roles.Florist))
                await _users.AddToRoleAsync(existingUser, Roles.Florist);

            if (string.IsNullOrWhiteSpace(existingUser.FullName) && !string.IsNullOrWhiteSpace(request.FloristFullName))
            {
                existingUser.FullName = request.FloristFullName.Trim();
                await _users.UpdateAsync(existingUser);
            }

            await _memberships.EnsureMembershipAsync(existingUser.Id, tenant.Id, ct);
        }
        else
        {
            var florist = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = floristEmail,
                Email = floristEmail,
                FullName = request.FloristFullName.Trim(),
                TenantId = tenant.Id,
                EmailConfirmed = false
            };

            var createResult = await _users.CreateAsync(florist, GenerateInternalPassword());
            if (!createResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

            await _users.AddToRoleAsync(florist, Roles.Florist);
            await _memberships.EnsureMembershipAsync(florist.Id, tenant.Id, ct);
            devInviteLink = await _invites.SendInviteAsync(florist, tenant, ct);
            floristInviteSent = true;
        }

        await _audit.LogAsync("PlatformTenantCreated", superAdminUserId, tenant.Id, nameof(Tenant), tenant.Id, slug, ct);

        var detail = MapDetail(tenant, 0);
        return new CreatePlatformTenantResponse(detail, devInviteLink, floristInviteSent);
    }

    public async Task<ResendFloristInviteResponse> ResendFloristInviteAsync(
        Guid tenantId,
        Guid superAdminUserId,
        CancellationToken ct = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdReadOnlyAsync(tenantId, ct)
            ?? throw new KeyNotFoundException("Loja não encontrada.");

        var florist = await _memberships.FindFloristUserForTenantAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Não foi encontrada florista para esta loja.");

        florist.EmailConfirmed = false;
        await _users.UpdateAsync(florist);

        var devLink = await _invites.SendInviteAsync(florist, tenant, ct);
        await _audit.LogAsync("FloristInviteResent", superAdminUserId, tenant.Id, nameof(ApplicationUser), florist.Id, tenant.Slug, ct);
        return new ResendFloristInviteResponse(devLink);
    }

    public async Task<PlatformTenantDetailDto> UpdateAsync(
        Guid id,
        UpdatePlatformTenantRequest request,
        Guid superAdminUserId,
        CancellationToken ct = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdWithSettingsForUpdateAsync(id, ct)
            ?? throw new KeyNotFoundException("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("O nome da loja é obrigatório.");

        StoreThemeRules.EnsureValidTheme(request.ThemePrimaryColor, request.ThemeAccentColor, request.ThemeFontFamily);

        var customDomain = TenantHostResolution.NormalizeCustomDomain(request.CustomDomain);
        if (customDomain is not null && await _unitOfWork.Tenants.CustomDomainExistsAsync(customDomain, id, ct))
            throw new InvalidOperationException("Este domínio próprio já está associado a outra loja.");

        var domainChanged = !string.Equals(
            tenant.CustomDomain,
            customDomain,
            StringComparison.OrdinalIgnoreCase);

        tenant.Name = request.Name.Trim();
        tenant.CustomDomain = customDomain;
        tenant.ThemePrimaryColor = request.ThemePrimaryColor;
        tenant.ThemeAccentColor = request.ThemeAccentColor;
        tenant.ThemeFontFamily = string.IsNullOrWhiteSpace(request.ThemeFontFamily)
            ? null
            : request.ThemeFontFamily.Trim().ToLowerInvariant();
        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        if (domainChanged)
            _corsCache.Invalidate();

        await _audit.LogAsync("PlatformTenantUpdated", superAdminUserId, tenant.Id, nameof(Tenant), tenant.Id, tenant.Slug, ct);

        var orderCounts = await _unitOfWork.Tenants.CountOrdersByTenantAsync(ct);
        return MapDetail(tenant, orderCounts.GetValueOrDefault(tenant.Id));
    }

    public async Task<PlatformTenantDetailDto> SetSuspendedAsync(
        Guid id,
        bool suspended,
        Guid superAdminUserId,
        CancellationToken ct = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdWithSettingsForUpdateAsync(id, ct)
            ?? throw new KeyNotFoundException("Loja não encontrada.");

        tenant.IsActive = !suspended;
        if (suspended)
            tenant.OrdersAcceptingEnabled = false;
        else if (!tenant.OrdersAcceptingEnabled)
            tenant.OrdersAcceptingEnabled = true;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        var action = suspended ? "PlatformTenantSuspended" : "PlatformTenantReactivated";
        await _audit.LogAsync(action, superAdminUserId, tenant.Id, nameof(Tenant), tenant.Id, tenant.Slug, ct);

        var orderCounts = await _unitOfWork.Tenants.CountOrdersByTenantAsync(ct);
        return MapDetail(tenant, orderCounts.GetValueOrDefault(tenant.Id));
    }

    private static void ApplyDefaultStoreSettings(Tenant tenant)
    {
        foreach (var day in Enumerable.Range(0, 7))
        {
            var closed = day is 0;
            tenant.OpeningHours.Add(new TenantOpeningHour
            {
                TenantId = tenant.Id,
                DayOfWeek = (DayOfWeek)day,
                IsClosed = closed,
                OpenTime = closed ? null : new TimeOnly(9, 0),
                CloseTime = closed ? null : new TimeOnly(19, 0)
            });
        }

        tenant.DeliveryZones.Add(new DeliveryZone
        {
            TenantId = tenant.Id,
            Name = "Zona geral",
            Fee = 0m,
            IsActive = true,
            SortOrder = 0
        });
    }

    private async Task EnsureRoleAsync(string role, CancellationToken ct)
    {
        if (!await _roles.RoleExistsAsync(role))
            await _roles.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = role });
    }

    private static string GenerateInternalPassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes) + "Aa1!";
    }

    private static PlatformTenantDetailDto MapDetail(Tenant t, int orderCount) =>
        new(
            t.Id,
            t.Name,
            t.Slug,
            t.CustomDomain,
            t.ContactEmail,
            t.ContactPhone,
            t.IsActive,
            t.IsPublished,
            t.OnboardingWizardStep,
            orderCount,
            t.ThemePrimaryColor,
            t.ThemeAccentColor,
            t.ThemeFontFamily,
            t.CreatedAt);
}
