using FluentAssertions;
using ProjetoPizza.Domain.Inventory;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Inventory;

public sealed class StockBalanceTests
{
    [Fact]
    public void ApplyAdjustment_ShouldUpdateAvailableQuantity()
    {
        var balance = new StockBalance(StockBalanceId.New(), InventoryItemId.New());

        balance.ApplyAdjustment(8.5m);
        balance.ApplyAdjustment(-2m);

        balance.CurrentQuantity.Should().Be(6.5m);
        balance.AvailableQuantity.Should().Be(6.5m);
    }

    [Fact]
    public void ApplyAdjustment_BelowAvailableBalance_ShouldReject()
    {
        var balance = new StockBalance(StockBalanceId.New(), InventoryItemId.New());
        balance.ApplyAdjustment(3m);

        var action = () => balance.ApplyAdjustment(-3.1m);

        action.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("stock_balance.insufficient");
        balance.CurrentQuantity.Should().Be(3m);
    }

    [Fact]
    public void StockMovement_ShouldKeepCostAndOrderItemTrace()
    {
        var orderItemId = OrderItemId.New();

        var movement = new StockMovement(
            StockMovementId.New(), InventoryItemId.New(), StockMovementType.Consumption,
            1.25m, new Money(8.40m), "Consumo do pedido", EmployeeId.New(), orderItemId);

        movement.UnitCost.Should().Be(new Money(8.40m));
        movement.OrderItemId.Should().Be(orderItemId);
    }
}
