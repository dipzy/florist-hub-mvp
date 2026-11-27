using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

public class Order : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public ApplicationUser? Customer { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PendingConfirmation;
    public ArrangementBaseType BaseType { get; set; }
    public ArrangementShape ArrangementShape { get; set; } = ArrangementShape.Sponge;
    public int ArrangementVariationSeed { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public string? DeliveryAddress { get; set; }
    public DateTimeOffset? RequestedDeliveryAt { get; set; }
    public string? CustomerNotes { get; set; }
    /// <summary>NIF do cliente, quando o pede na fatura. Vazio significa consumidor final.</summary>
    public string? CustomerTaxId { get; set; }
    public Guid? InspiredByPortfolioArrangementId { get; set; }
    public PortfolioArrangement? InspiredByPortfolioArrangement { get; set; }

    /// <summary>Valor a pagar, já com o desconto do cupão.</summary>
    public decimal EstimatedTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public Guid? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public string? CouponCode { get; set; }
    public decimal DeliveryFee { get; set; }
    public Guid? DeliveryZoneId { get; set; }
    public DeliveryZone? DeliveryZone { get; set; }
    public string? FloristInternalNotes { get; set; }

    /// <summary>Referência do pagamento Stripe (sessão de Checkout), quando existir.</summary>
    public string? PaymentReference { get; set; }
    public bool PaymentCaptured { get; set; }

    public DateOnly? CapacityServiceDate { get; set; }
    public string? CapacityWindowKey { get; set; }
    public bool CapacityHeld { get; set; }
    public bool StockCommitted { get; set; }

    /// <summary>Token não adivinhável para acompanhamento público da encomenda (link no email).</summary>
    public string? TrackingAccessToken { get; set; }

    /// <summary>Chave de idempotência do cliente (cabeçalho Idempotency-Key) para evitar duplicados.</summary>
    public string? ClientSubmissionId { get; set; }

    public string? AcceptedTermsVersion { get; set; }
    public string? AcceptedPrivacyVersion { get; set; }
    public DateTimeOffset? LegalConsentRecordedAt { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>Token com a semente e as opções do arranjo 3D, para reconstruir a mesma variação.</summary>
    public string? Arrangement3DPreviewToken { get; set; }

    /// <summary>PNG gerado no canvas do configurador e enviado pelo upload de ficheiros.</summary>
    public string? ArrangementPreviewUrl { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<OrderChangeProposal> ChangeProposals { get; set; } = new List<OrderChangeProposal>();
    public OrderReview? Review { get; set; }

    public Guid? RecurringSubscriptionId { get; set; }
    public RecurringSubscription? RecurringSubscription { get; set; }
    public Guid? RecurringOccurrenceId { get; set; }
    public RecurringOccurrence? RecurringOccurrence { get; set; }
}
