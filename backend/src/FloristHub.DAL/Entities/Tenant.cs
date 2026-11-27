using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class Tenant : BaseEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>A loja pública só fica visível quando publicada e com requisitos obrigatórios cumpridos.</summary>
    public bool IsPublished { get; set; }

    /// <summary>Passo atual do assistente de configuração (chave estável).</summary>
    public string? OnboardingWizardStep { get; set; }

    public DateTimeOffset? PlatformTermsAcceptedAt { get; set; }

    /// <summary>Florista optou por não aceitar pagamentos online (válido para publicar).</summary>
    public bool PaymentsDisabledByChoice { get; set; }

    /// <summary>Cores confirmadas no assistente (requisito de publicação).</summary>
    public bool OnboardingColorsAcknowledged { get; set; }

    public string? LogoUrl { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    public bool OrdersAcceptingEnabled { get; set; } = true;
    public int MinimumOrderAdvanceMinutes { get; set; } = 120;
    public bool PickupEnabled { get; set; } = true;
    public bool HomeDeliveryEnabled { get; set; } = true;
    public bool CemeteryDeliveryEnabled { get; set; } = true;

    /// <summary>Domínio próprio (host completo, sem protocolo), opcional e único.</summary>
    public string? CustomDomain { get; set; }

    public string ThemePrimaryColor { get; set; } = "#2d6a4f";
    public string ThemeAccentColor { get; set; } = "#95d5b2";
    /// <summary>Chave de tipografia (ex. system, serif, rounded).</summary>
    public string? ThemeFontFamily { get; set; }

    /// <summary>Conta Stripe Connect Express da florista.</summary>
    public string? StripeAccountId { get; set; }
    public bool StripeChargesEnabled { get; set; }
    public bool StripePayoutsEnabled { get; set; }
    public bool StripeDetailsSubmitted { get; set; }
    public bool StripeCardPayments { get; set; }
    public bool StripeMbWay { get; set; }
    public bool StripeMultibanco { get; set; }

    /// <summary>Percentagem de comissão da plataforma. Nulo usa o valor global.</summary>
    public decimal? CommissionPercentOverride { get; set; }

    /// <summary>Mensalidade prevista para esta loja. Ainda não é cobrada.</summary>
    public decimal? MonthlyFeeAmount { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<Flower> Flowers { get; set; } = new List<Flower>();
    public ICollection<TenantOpeningHour> OpeningHours { get; set; } = new List<TenantOpeningHour>();
    public ICollection<DeliveryZone> DeliveryZones { get; set; } = new List<DeliveryZone>();
}
