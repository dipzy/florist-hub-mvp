using FloristHub.DAL.Enums;
using FloristHub.Services.Orders;

namespace FloristHub.Services.Tests;

public class OrderStateMachineTests
{
    public static TheoryData<OrderStatus, OrderStatus, bool> TransitionMatrix() =>
        new()
        {
            // Pendente
            { OrderStatus.PendingConfirmation, OrderStatus.Confirmed, true },
            { OrderStatus.PendingConfirmation, OrderStatus.Rejected, true },
            { OrderStatus.PendingConfirmation, OrderStatus.Cancelled, true },
            { OrderStatus.PendingConfirmation, OrderStatus.InPreparation, false },
            { OrderStatus.PendingConfirmation, OrderStatus.Delivered, false },

            // Confirmada
            { OrderStatus.Confirmed, OrderStatus.InPreparation, true },
            { OrderStatus.Confirmed, OrderStatus.Cancelled, true },
            { OrderStatus.Confirmed, OrderStatus.Ready, false },

            // Preparação → pronta → entregue
            { OrderStatus.InPreparation, OrderStatus.Ready, true },
            { OrderStatus.InPreparation, OrderStatus.Delivered, false },
            { OrderStatus.Ready, OrderStatus.Delivered, true },
            { OrderStatus.Ready, OrderStatus.InPreparation, false },

            // Estados finais
            { OrderStatus.Delivered, OrderStatus.Cancelled, false },
            { OrderStatus.Cancelled, OrderStatus.Confirmed, false },
            { OrderStatus.Rejected, OrderStatus.Confirmed, false },
        };

    [Theory]
    [MemberData(nameof(TransitionMatrix))]
    public void CanTransition_respects_business_rules(OrderStatus from, OrderStatus to, bool expected) =>
        Assert.Equal(expected, OrderStateMachine.CanTransition(from, to));

    [Fact]
    public void EnsureCanTransition_throws_on_invalid_transition()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            OrderStateMachine.EnsureCanTransition(OrderStatus.PendingConfirmation, OrderStatus.Delivered));
        Assert.Contains("Transição inválida", ex.Message);
    }

    [Theory]
    [InlineData(OrderStatus.PendingConfirmation)]
    [InlineData(OrderStatus.Confirmed)]
    public void Customer_can_cancel_from_early_states(OrderStatus from)
    {
        Assert.True(OrderStateMachine.CanTransition(from, OrderStatus.Cancelled));
    }
}
