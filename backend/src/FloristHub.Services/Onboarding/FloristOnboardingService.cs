using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Capacity;
using FloristHub.Services.DTOs.Capacity;
using FloristHub.Services.DTOs.Onboarding;
using FloristHub.Services.Legal;
using FloristHub.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Onboarding;

public interface IFloristOnboardingService
{
    Task<FloristOnboardingStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<FloristOnboardingStatusDto> SetWizardStepAsync(string step, Guid floristUserId, CancellationToken ct = default);
    Task<FloristOnboardingStatusDto> AcknowledgeColorsAsync(Guid floristUserId, CancellationToken ct = default);
    Task<FloristOnboardingStatusDto> SetDailyCapacityAsync(int dailyOrderLimit, Guid floristUserId, CancellationToken ct = default);
    Task<FloristOnboardingStatusDto> SetPaymentsDisabledByChoiceAsync(bool disabled, Guid floristUserId, CancellationToken ct = default);
    Task<FloristOnboardingStatusDto> AcceptPlatformTermsAsync(Guid floristUserId, CancellationToken ct = default);
    Task<FloristOnboardingStatusDto> PublishAsync(Guid floristUserId, CancellationToken ct = default);
    Task<PlatformTenantOnboardingDto> GetPlatformViewAsync(Guid tenantId, CancellationToken ct = default);
}

public sealed class FloristOnboardingService : IFloristOnboardingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;
    private readonly StripeOptions _stripe;
    private readonly IDeliveryCapacityService _capacity;

    public FloristOnboardingService(
        IUnitOfWork unitOfWork,
        ITenantContext tenant,
        IAuditLogger audit,
        IOptions<StripeOptions> stripe,
        IDeliveryCapacityService capacity)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _audit = audit;
        _stripe = stripe.Value;
        _capacity = capacity;
    }

    public Task<FloristOnboardingStatusDto> GetStatusAsync(CancellationToken ct = default) =>
        BuildStatusAsync(ct);

    public async Task<FloristOnboardingStatusDto> SetWizardStepAsync(string step, Guid floristUserId, CancellationToken ct = default)
    {
        if (!FloristOnboardingSteps.Ordered.Contains(step))
            throw new InvalidOperationException("Passo de configuração inválido.");

        var tenant = await RequireTenantForUpdateAsync(ct);
        tenant.OnboardingWizardStep = step;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("OnboardingWizardStep", floristUserId, tenant.Id, nameof(Tenant), tenant.Id, step, ct);
        return await BuildStatusAsync(ct);
    }

    public async Task<FloristOnboardingStatusDto> AcknowledgeColorsAsync(Guid floristUserId, CancellationToken ct = default)
    {
        var tenant = await RequireTenantForUpdateAsync(ct);
        tenant.OnboardingColorsAcknowledged = true;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("OnboardingColorsAcknowledged", floristUserId, tenant.Id, nameof(Tenant), tenant.Id, null, ct);
        return await BuildStatusAsync(ct);
    }

    public async Task<FloristOnboardingStatusDto> SetDailyCapacityAsync(int dailyOrderLimit, Guid floristUserId, CancellationToken ct = default)
    {
        if (dailyOrderLimit < 1)
            throw new InvalidOperationException("A capacidade diária tem de ser pelo menos 1.");

        await _capacity.UpdateSettingsAsync(
            new UpdateDeliveryCapacitySettingsRequest(
                dailyOrderLimit,
                [
                    new DeliveryWindowInput("Todo o dia", "09:00", "19:00", dailyOrderLimit),
                ]),
            floristUserId,
            ct);
        await _audit.LogAsync(
            "OnboardingDailyCapacity",
            floristUserId,
            _tenant.TenantId,
            nameof(DeliveryCapacityPolicy),
            null,
            dailyOrderLimit.ToString(),
            ct);
        return await BuildStatusAsync(ct);
    }

    public async Task<FloristOnboardingStatusDto> SetPaymentsDisabledByChoiceAsync(bool disabled, Guid floristUserId, CancellationToken ct = default)
    {
        var tenant = await RequireTenantForUpdateAsync(ct);
        tenant.PaymentsDisabledByChoice = disabled;
        if (disabled)
            tenant.OrdersAcceptingEnabled = true;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync(
            disabled ? "OnboardingPaymentsDisabledByChoice" : "OnboardingPaymentsReenabled",
            floristUserId,
            tenant.Id,
            nameof(Tenant),
            tenant.Id,
            null,
            ct);
        return await BuildStatusAsync(ct);
    }

    public async Task<FloristOnboardingStatusDto> AcceptPlatformTermsAsync(Guid floristUserId, CancellationToken ct = default)
    {
        var tenant = await RequireTenantForUpdateAsync(ct);
        tenant.PlatformTermsAcceptedAt = DateTimeOffset.UtcNow;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync(ComplianceAuditActions.TermsAcceptedRegistration, floristUserId, tenant.Id, nameof(Tenant), tenant.Id, "platform-florist", ct);
        return await BuildStatusAsync(ct);
    }

    public async Task<FloristOnboardingStatusDto> PublishAsync(Guid floristUserId, CancellationToken ct = default)
    {
        var tenant = await RequireTenantForUpdateAsync(ct);
        var hasFlower = await TenantPublicationRules.HasCatalogFlowerAsync(
            _unitOfWork.Flowers.Query(),
            tenant.Id,
            ct);
        var stripeConfigured = !string.IsNullOrWhiteSpace(_stripe.SecretKey);
        var missing = TenantPublicationRules.MissingRequiredForPublish(tenant, hasFlower, stripeConfigured);
        if (missing.Count > 0)
            throw new InvalidOperationException("Ainda faltam passos obrigatórios: " + string.Join(", ", missing) + ".");

        tenant.IsPublished = true;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("StorePublished", floristUserId, tenant.Id, nameof(Tenant), tenant.Id, null, ct);
        return await BuildStatusAsync(ct);
    }

    public async Task<PlatformTenantOnboardingDto> GetPlatformViewAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdWithSettingsReadOnlyAsync(tenantId, ct)
            ?? throw new KeyNotFoundException("Loja não encontrada.");

        var status = await BuildStatusForTenantAsync(tenant, ct);
        var required = status.Checklist.Where(c => c.IsRequired).ToList();
        var floristCompleted = await _unitOfWork.FloristInvites.Query()
            .AnyAsync(i => i.TenantId == tenantId && i.UsedAt != null, ct);

        return new PlatformTenantOnboardingDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.IsPublished,
            floristCompleted,
            tenant.OnboardingWizardStep,
            status.Checklist,
            required.Count(c => c.IsComplete),
            required.Count);
    }

    private async Task<FloristOnboardingStatusDto> BuildStatusAsync(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant não definido.");
        var tenant = await _unitOfWork.Tenants.GetByIdWithSettingsReadOnlyAsync(tenantId, ct)
            ?? throw new KeyNotFoundException("Loja não encontrada.");
        return await BuildStatusForTenantAsync(tenant, ct);
    }

    private async Task<FloristOnboardingStatusDto> BuildStatusForTenantAsync(Tenant tenant, CancellationToken ct)
    {
        var hasFlower = await TenantPublicationRules.HasCatalogFlowerAsync(
            _unitOfWork.Flowers.Query(),
            tenant.Id,
            ct);
        var stripeConfigured = !string.IsNullOrWhiteSpace(_stripe.SecretKey);
        var invoicing = await _unitOfWork.InvoicingSettings.Query()
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id, ct);
        var invoicingConfigured = IsInvoicingConfigured(invoicing);
        var capacityDone = await HasCapacityConfiguredAsync(tenant.Id, ct);
        var checklist = BuildChecklist(tenant, hasFlower, stripeConfigured, invoicingConfigured, capacityDone);
        var missing = TenantPublicationRules.MissingRequiredForPublish(tenant, hasFlower, stripeConfigured);
        string step;
        int progress;
        if (tenant.IsPublished)
        {
            step = FloristOnboardingSteps.Ordered[^1];
            progress = 100;
        }
        else
        {
            step = string.IsNullOrWhiteSpace(tenant.OnboardingWizardStep)
                ? FloristOnboardingSteps.DefaultFirst
                : tenant.OnboardingWizardStep!;
            var stepIndex = Math.Max(0, Array.IndexOf(FloristOnboardingSteps.Ordered, step));
            progress = (int)Math.Round(100.0 * (stepIndex + 1) / FloristOnboardingSteps.Ordered.Length);
        }

        return new FloristOnboardingStatusDto(
            tenant.IsPublished,
            missing.Count == 0,
            missing,
            checklist,
            step,
            progress);
    }

    private async Task<bool> HasCapacityConfiguredAsync(Guid tenantId, CancellationToken ct)
    {
        var policy = await _unitOfWork.CapacityPolicies.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, ct);
        if (policy is null || policy.DailyOrderLimit < 1)
            return false;

        return await _unitOfWork.CapacityWindows.Query()
            .IgnoreQueryFilters()
            .AnyAsync(w => w.TenantId == tenantId, ct);
    }

    private static bool IsInvoicingConfigured(FloristInvoicingSettings? invoicing)
    {
        if (invoicing is null)
            return false;
        if (string.IsNullOrWhiteSpace(invoicing.LegalName) || string.IsNullOrWhiteSpace(invoicing.TaxId))
            return false;
        if (!invoicing.Enabled)
            return true;
        if (invoicing.Provider == InvoicingProviderKind.InvoiceXpress)
            return !string.IsNullOrWhiteSpace(invoicing.AccountName)
                && !string.IsNullOrWhiteSpace(invoicing.ProtectedApiKey);
        return true;
    }

    private static List<OnboardingChecklistItemDto> BuildChecklist(
        Tenant tenant,
        bool hasFlower,
        bool stripeConfigured,
        bool invoicingConfigured,
        bool capacityDone)
    {
        return
        [
            Item("dados-loja", "Dados da loja", "Nome, contactos e morada.", true, TenantPublicationRules.HasShopBasics(tenant), "/admin/definicoes", FloristOnboardingSteps.Loja),
            Item("logo", "Logótipo", "Imagem da marca na loja.", false, !string.IsNullOrWhiteSpace(tenant.LogoUrl), "/admin/definicoes", FloristOnboardingSteps.Loja),
            Item("cores", "Cores da loja", "Tema visual confirmado.", true, TenantPublicationRules.HasColors(tenant), "/admin/configuracao-inicial", FloristOnboardingSteps.Cores),
            Item("dominio", "Domínio", "Subdomínio e domínio próprio.", false, !string.IsNullOrWhiteSpace(tenant.Slug), "/admin/configuracao-inicial", FloristOnboardingSteps.Dominio),
            Item("horario", "Horário", "Horário de funcionamento.", false, tenant.OpeningHours.Any(h => !h.IsClosed), "/admin/definicoes", FloristOnboardingSteps.ContactosHorario),
            Item("zona-entrega", "Zonas de entrega", "Pelo menos uma zona ativa.", true, TenantPublicationRules.HasActiveDeliveryZone(tenant), "/admin/definicoes", FloristOnboardingSteps.Entrega),
            Item("capacidade", "Capacidade diária", "Limite diário e janelas de entrega.", false, capacityDone, "/admin/capacidade", FloristOnboardingSteps.Capacidade),
            Item("catalogo", "Catálogo", "Pelo menos uma flor ativa com stock.", true, hasFlower, "/admin/catalogo", FloristOnboardingSteps.Catalogo),
            Item("fiscal", "Faturação", "Dados fiscais e InvoiceXpress (opcional).", false, invoicingConfigured, "/admin/faturacao", FloristOnboardingSteps.Fiscal),
            Item("pagamentos", "Pagamentos", "Stripe ligado ou pagamentos desativados.", true, TenantPublicationRules.PaymentsRequirementMet(tenant, stripeConfigured), "/admin/pagamentos", FloristOnboardingSteps.Pagamentos),
            Item("termos-plataforma", "Termos da plataforma", "Aceitação dos termos Florist Hub.", true, TenantPublicationRules.HasTerms(tenant), "/admin/configuracao-inicial", FloristOnboardingSteps.Termos),
        ];
    }

    private static OnboardingChecklistItemDto Item(
        string key,
        string title,
        string description,
        bool required,
        bool complete,
        string? adminPath,
        string? wizardStep) =>
        new(key, title, description, required, complete, adminPath, wizardStep);

    private async Task<Tenant> RequireTenantForUpdateAsync(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant não definido.");
        return await _unitOfWork.Tenants.GetByIdWithSettingsForUpdateAsync(tenantId, ct)
            ?? throw new KeyNotFoundException("Loja não encontrada.");
    }
}
