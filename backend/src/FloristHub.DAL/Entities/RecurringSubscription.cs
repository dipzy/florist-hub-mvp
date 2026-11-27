using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

public class RecurringSubscription : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public ApplicationUser? Customer { get; set; }

    public RecurringSubscriptionStatus Status { get; set; } = RecurringSubscriptionStatus.PendingFloristApproval;
    public RecurringFrequency Frequency { get; set; }

    /// <summary>Primeira entrega acordada (âncora do calendário).</summary>
    public DateTimeOffset AnchorDeliveryAt { get; set; }

    /// <summary>Próxima entrega prevista (atualizada após cada ciclo).</summary>
    public DateTimeOffset? NextDeliveryAt { get; set; }

    public DeliveryType DeliveryType { get; set; }
    public string? DeliveryAddress { get; set; }
    public Guid? DeliveryZoneId { get; set; }
    public DeliveryZone? DeliveryZone { get; set; }

    public ArrangementBaseType BaseType { get; set; }
    public ArrangementShape ArrangementShape { get; set; } = ArrangementShape.Sponge;
    public int ArrangementVariationSeed { get; set; }
    public string? ArrangementPreviewUrl { get; set; }
    public string? CustomerNotes { get; set; }

    /// <summary>Quando verdadeiro, a florista compõe dentro do orçamento indicado.</summary>
    public bool IsFloristChoice { get; set; }
    public decimal? FloristChoiceBudget { get; set; }

    public string? FloristResponseNotes { get; set; }

    public string? AcceptedTermsVersion { get; set; }
    public string? AcceptedPrivacyVersion { get; set; }
    public DateTimeOffset? LegalConsentRecordedAt { get; set; }

    /// <summary>Bloqueio curto para geração concorrente entre instâncias.</summary>
    public DateTimeOffset? GenerationLeaseUntil { get; set; }

    public ICollection<RecurringSubscriptionItem> Items { get; set; } = new List<RecurringSubscriptionItem>();
    public ICollection<RecurringOccurrence> Occurrences { get; set; } = new List<RecurringOccurrence>();
}
