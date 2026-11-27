using FloristHub.DAL.Audit;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Entities;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Auth;
using FloristHub.Services.DTOs.Auth;
using FloristHub.Services.Email;
using FloristHub.Services.Interfaces.Email;
using FloristHub.Services.Onboarding;
using Microsoft.AspNetCore.Identity;

namespace FloristHub.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> SwitchContextAsync(Guid userId, SwitchContextRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AuthContextOption>> GetAvailableContextsAsync(Guid userId, CancellationToken ct = default);
    Task CompleteFloristInviteAsync(CompleteFloristInviteRequest request, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private const string LoginInvalidCredentials = "Email ou palavra-passe incorretos.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IAuditLogger _audit;
    private readonly IAuthEmailNotifications _emails;
    private readonly ILegalComplianceService _legal;
    private readonly IFloristInviteStore _invites;
    private readonly IFloristMembershipService _memberships;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwt,
        IAuditLogger audit,
        IAuthEmailNotifications emails,
        ILegalComplianceService legal,
        IFloristInviteStore invites,
        IFloristMembershipService memberships)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
        _jwt = jwt;
        _audit = audit;
        _emails = emails;
        _legal = legal;
        _invites = invites;
        _memberships = memberships;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (!IsValidRole(request.Role))
            throw new InvalidOperationException("Role inválida.");

        var tenantId = await ResolveTenantForRegistrationAsync(request, ct);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            TenantId = tenantId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await EnsureRoleExistsAsync(request.Role);
        await _userManager.AddToRoleAsync(user, request.Role);
        if (request.Role == Roles.Florist)
            await _memberships.EnsureMembershipAsync(user.Id, tenantId, ct);
        await _legal.RecordRegistrationConsentsAsync(user, request, ct);
        await _userManager.UpdateAsync(user);

        var tenant = await _unitOfWork.Tenants.GetByIdReadOnlyAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant não encontrado.");
        EnsureTenantAllowsLogin(tenant, request.Role);
        var (token, expires) = _jwt.GenerateToken(user, request.Role, tenant);

        await _audit.LogAsync("UserRegistered", user.Id, tenantId, nameof(ApplicationUser), user.Id, request.Role, ct);
        _emails.WelcomeRegistered(user, tenant, request.Role);
        var roles = await _userManager.GetRolesAsync(user);
        var contexts = await BuildAvailableContextsAsync(user.Id, roles, ct);
        return new AuthResponse(token, expires, user.Id, user.Email!, request.Role, tenant.Id, tenant.Slug, contexts);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException(LoginInvalidCredentials);

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException(LoginInvalidCredentials);

        var roles = (await _userManager.GetRolesAsync(user)).ToHashSet(StringComparer.Ordinal);
        var platformLogin = !request.TenantId.HasValue && string.IsNullOrWhiteSpace(request.TenantSlug);

        if (platformLogin)
        {
            if (!roles.Contains(Roles.SuperAdmin))
                throw new UnauthorizedAccessException(LoginInvalidCredentials);

            if (user.TenantId.HasValue)
                throw new InvalidOperationException("SuperAdmin não pode estar associado a uma loja.");

            var (token, expires) = _jwt.GenerateToken(user, Roles.SuperAdmin, null);
            await _audit.LogAsync("UserLogin", user.Id, null, nameof(ApplicationUser), user.Id, Roles.SuperAdmin, ct);
            var contexts = await BuildAvailableContextsAsync(user.Id, roles, ct);
            return new AuthResponse(token, expires, user.Id, user.Email!, Roles.SuperAdmin, null, null, contexts);
        }

        var tenant = await ResolveTenantForLoginAsync(request, ct);

        if (roles.Contains(Roles.SuperAdmin) && !user.TenantId.HasValue)
        {
            var (saToken, saExpires) = _jwt.GenerateToken(user, Roles.SuperAdmin, null);
            await _audit.LogAsync("UserLogin", user.Id, null, nameof(ApplicationUser), user.Id, Roles.SuperAdmin, ct);
            var saContexts = await BuildAvailableContextsAsync(user.Id, roles, ct);
            return new AuthResponse(saToken, saExpires, user.Id, user.Email!, Roles.SuperAdmin, null, null, saContexts);
        }

        if (roles.Contains(Roles.Florist) && await _memberships.HasMembershipAsync(user.Id, tenant.Id, ct))
        {
            if (!user.EmailConfirmed)
                throw new UnauthorizedAccessException(
                    "Confirme o convite por email e defina a palavra-passe antes de iniciar sessão.");

            EnsureTenantAllowsLogin(tenant, Roles.Florist);
            var (floristToken, floristExpires) = _jwt.GenerateToken(user, Roles.Florist, tenant);
            await _audit.LogAsync("UserLogin", user.Id, tenant.Id, nameof(ApplicationUser), user.Id, Roles.Florist, ct);
            var contexts = await BuildAvailableContextsAsync(user.Id, roles, ct);
            return new AuthResponse(floristToken, floristExpires, user.Id, user.Email!, Roles.Florist, tenant.Id, tenant.Slug, contexts);
        }

        if (roles.Contains(Roles.Customer))
        {
            if (!user.TenantId.HasValue || user.TenantId != tenant.Id)
                throw new UnauthorizedAccessException(LoginInvalidCredentials);

            EnsureTenantAllowsLogin(tenant, Roles.Customer);
            var (customerToken, customerExpires) = _jwt.GenerateToken(user, Roles.Customer, tenant);
            await _audit.LogAsync("UserLogin", user.Id, tenant.Id, nameof(ApplicationUser), user.Id, Roles.Customer, ct);
            var contexts = await BuildAvailableContextsAsync(user.Id, roles, ct);
            return new AuthResponse(customerToken, customerExpires, user.Id, user.Email!, Roles.Customer, tenant.Id, tenant.Slug, contexts);
        }

        throw new UnauthorizedAccessException(LoginInvalidCredentials);
    }

    public async Task<AuthResponse> SwitchContextAsync(Guid userId, SwitchContextRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedAccessException("Sessão inválida.");

        var roles = (await _userManager.GetRolesAsync(user)).ToHashSet(StringComparer.Ordinal);
        var contexts = await BuildAvailableContextsAsync(user.Id, roles, ct);

        if (string.Equals(request.Kind, AuthContextKinds.SuperAdmin, StringComparison.Ordinal))
        {
            if (!roles.Contains(Roles.SuperAdmin) || user.TenantId.HasValue)
                throw new UnauthorizedAccessException("Não tem permissão para o contexto SuperAdmin.");

            var (token, expires) = _jwt.GenerateToken(user, Roles.SuperAdmin, null);
            await _audit.LogAsync("UserContextSwitch", user.Id, null, nameof(ApplicationUser), user.Id, Roles.SuperAdmin, ct);
            return new AuthResponse(token, expires, user.Id, user.Email!, Roles.SuperAdmin, null, null, contexts);
        }

        if (string.Equals(request.Kind, AuthContextKinds.Florist, StringComparison.Ordinal))
        {
            if (!request.TenantId.HasValue)
                throw new InvalidOperationException("É necessário indicar a loja.");

            if (!roles.Contains(Roles.Florist) ||
                !await _memberships.HasMembershipAsync(user.Id, request.TenantId.Value, ct))
                throw new UnauthorizedAccessException("Não tem permissão para esta loja.");

            if (!user.EmailConfirmed)
                throw new UnauthorizedAccessException(
                    "Confirme o convite por email e defina a palavra-passe antes de aceder à loja.");

            var tenant = await _unitOfWork.Tenants.GetByIdReadOnlyAsync(request.TenantId.Value, ct)
                ?? throw new KeyNotFoundException("Loja não encontrada.");

            EnsureTenantAllowsLogin(tenant, Roles.Florist);
            var (token, expires) = _jwt.GenerateToken(user, Roles.Florist, tenant);
            await _audit.LogAsync("UserContextSwitch", user.Id, tenant.Id, nameof(ApplicationUser), user.Id, Roles.Florist, ct);
            return new AuthResponse(token, expires, user.Id, user.Email!, Roles.Florist, tenant.Id, tenant.Slug, contexts);
        }

        throw new InvalidOperationException("Contexto inválido.");
    }

    public Task<IReadOnlyList<AuthContextOption>> GetAvailableContextsAsync(Guid userId, CancellationToken ct = default)
    {
        return BuildAvailableContextsInternalAsync(userId, ct);
    }

    public async Task CompleteFloristInviteAsync(CompleteFloristInviteRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new InvalidOperationException("Token em falta.");

        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new InvalidOperationException("Convite inválido ou expirado.");

        if (!await _userManager.IsInRoleAsync(user, Roles.Florist))
            throw new InvalidOperationException("Este convite não é válido para esta conta.");

        var invite = await _invites.GetActiveInviteAsync(user.Id, ct);
        if (invite is null)
            throw new InvalidOperationException("Convite inválido ou expirado.");
        if (invite.ExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Convite expirado. Peça um novo convite.");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException("Convite inválido ou expirado.");

        await _invites.MarkUsedAsync(user.Id, ct);

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        if (user.TenantId is Guid tenantId)
        {
            await _memberships.EnsureMembershipAsync(user.Id, tenantId, ct);
            var tenant = await _unitOfWork.Tenants.GetByIdWithSettingsForUpdateAsync(tenantId, ct);
            if (tenant is not null)
            {
                tenant.OnboardingWizardStep ??= FloristOnboardingSteps.DefaultFirst;
                tenant.UpdatedAt = DateTimeOffset.UtcNow;
                await _unitOfWork.SaveChangesAsync(ct);
            }
        }

        await _audit.LogAsync("FloristInviteCompleted", user.Id, user.TenantId, nameof(ApplicationUser), user.Id, null, ct);
    }

    private async Task<IReadOnlyList<AuthContextOption>> BuildAvailableContextsInternalAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("Utilizador não encontrado.");

        var roles = (await _userManager.GetRolesAsync(user)).ToHashSet(StringComparer.Ordinal);
        var options = new List<AuthContextOption>();

        if (roles.Contains(Roles.SuperAdmin) && !user.TenantId.HasValue)
            options.Add(new AuthContextOption(AuthContextKinds.SuperAdmin, null, "SuperAdmin (plataforma)", null));

        if (roles.Contains(Roles.Florist))
        {
            var tenants = await _memberships.ListFloristTenantsAsync(userId, ct);
            foreach (var tenant in tenants)
                options.Add(new AuthContextOption(AuthContextKinds.Florist, tenant.Id, tenant.Name, tenant.Slug));
        }

        return options.Count <= 1 ? [] : options;
    }

    private async Task<IReadOnlyList<AuthContextOption>> BuildAvailableContextsAsync(Guid userId, IEnumerable<string> roles, CancellationToken ct)
    {
        _ = roles;
        return await BuildAvailableContextsInternalAsync(userId, ct);
    }

    private async Task<Tenant> ResolveTenantForLoginAsync(LoginRequest request, CancellationToken ct)
    {
        if (request.TenantId.HasValue)
        {
            return await _unitOfWork.Tenants.GetByIdReadOnlyAsync(request.TenantId.Value, ct)
                ?? throw new UnauthorizedAccessException(LoginInvalidCredentials);
        }

        if (!string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            var tenant = await _unitOfWork.Tenants.GetBySlugReadOnlyAsync(request.TenantSlug.Trim(), ct);
            if (tenant is null)
                throw new UnauthorizedAccessException(LoginInvalidCredentials);
            return tenant;
        }

        throw new UnauthorizedAccessException(LoginInvalidCredentials);
    }

    private async Task<Guid> ResolveTenantForRegistrationAsync(RegisterRequest request, CancellationToken ct)
    {
        if (request.Role == Roles.SuperAdmin)
            throw new InvalidOperationException("Registo de SuperAdmin não é permitido.");

        if (request.Role == Roles.Florist)
        {
            if (request.TenantId.HasValue)
                return request.TenantId.Value;

            if (string.IsNullOrWhiteSpace(request.NewTenantName) || string.IsNullOrWhiteSpace(request.NewTenantSlug))
                throw new InvalidOperationException("Florista novo requer NewTenantName e NewTenantSlug.");

            var slug = request.NewTenantSlug.Trim().ToLowerInvariant();
            if (await _unitOfWork.Tenants.SlugExistsAsync(slug, ct))
                throw new InvalidOperationException("Slug de tenant já existe.");

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.NewTenantName.Trim(),
                Slug = slug,
                IsActive = true
            };
            await _unitOfWork.Tenants.AddAsync(tenant, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return tenant.Id;
        }

        if (request.Role == Roles.Customer)
        {
            if (!request.TenantId.HasValue)
                throw new InvalidOperationException("Cliente requer TenantId da florista.");
            return request.TenantId.Value;
        }

        throw new InvalidOperationException("Role não suportada.");
    }

    private static bool IsValidRole(string role) =>
        role is Roles.Florist or Roles.Customer;

    private static void EnsureTenantAllowsLogin(Tenant tenant, string role)
    {
        if (tenant.IsActive)
            return;

        if (role == Roles.Florist)
            throw new UnauthorizedAccessException("Esta loja está suspensa. Contacte o suporte da plataforma.");

        throw new UnauthorizedAccessException("Esta loja não está disponível de momento.");
    }

    private async Task EnsureRoleExistsAsync(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = role });
    }
}

public interface IJwtTokenGenerator
{
    (string Token, DateTime Expires) GenerateToken(ApplicationUser user, string role, Tenant? tenant);
}
