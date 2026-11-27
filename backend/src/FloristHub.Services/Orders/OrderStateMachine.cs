using FloristHub.DAL.Enums;

namespace FloristHub.Services.Orders;

/// <summary>
/// Regras de transição de estado das encomendas (domínio puro, testável).
/// </summary>
public static class OrderStateMachine
{
    public static bool CanTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.PendingConfirmation, OrderStatus.Confirmed) => true,
        (OrderStatus.PendingConfirmation, OrderStatus.Rejected) => true,
        (OrderStatus.PendingConfirmation, OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed, OrderStatus.InPreparation) => true,
        (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
        (OrderStatus.InPreparation, OrderStatus.Ready) => true,
        (OrderStatus.Ready, OrderStatus.Delivered) => true,
        _ => false
    };

    public static void EnsureCanTransition(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException($"Transição inválida de {from} para {to}.");
    }
}
