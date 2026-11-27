using FloristHub.DAL.Enums;
using FloristHub.Services.Orders;

namespace FloristHub.Services.Tests;

/// <summary>
/// Testes de regras de negócio reutilizados pela camada Application.
/// </summary>
public class OrderStateMachineApplicationTests
{
    [Fact]
    public void Confirmed_order_can_enter_preparation()
    {
        Assert.True(OrderStateMachine.CanTransition(OrderStatus.Confirmed, OrderStatus.InPreparation));
    }
}
