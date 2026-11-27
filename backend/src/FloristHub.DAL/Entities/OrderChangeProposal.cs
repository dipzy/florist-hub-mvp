using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

public class OrderChangeProposal : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid ProposedByUserId { get; set; }
    public OrderChangeProposalStatus Status { get; set; } = OrderChangeProposalStatus.Pending;
    public required string Message { get; set; }
    /// <summary>JSON com substituições sugeridas (simples no MVP).</summary>
    public string? ProposedItemChangesJson { get; set; }
    public decimal? RevisedTotal { get; set; }
}
