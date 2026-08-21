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

    [Fact]
    public void Reservation_lifecycle_should_preserve_current_stock_until_consumption()
    {
        var balance = new StockBalance(StockBalanceId.New(), InventoryItemId.New());
        balance.ApplyAdjustment(10m);

        balance.Reserve(3m);

        balance.CurrentQuantity.Should().Be(10m);
        balance.ReservedQuantity.Should().Be(3m);
        balance.AvailableQuantity.Should().Be(7m);

        balance.ConsumeReserved(3m);

        balance.CurrentQuantity.Should().Be(7m);
        balance.ReservedQuantity.Should().Be(0m);
        balance.AvailableQuantity.Should().Be(7m);
    }

    [Fact]
    public void Released_reservation_should_restore_available_stock_without_changing_current_stock()
    {
        var balance = new StockBalance(StockBalanceId.New(), InventoryItemId.New());
        balance.ApplyAdjustment(5m);
        balance.Reserve(2m);

        balance.ReleaseReserved(2m);

        balance.CurrentQuantity.Should().Be(5m);
        balance.ReservedQuantity.Should().Be(0m);
        balance.AvailableQuantity.Should().Be(5m);
    }

    [Fact]
    public void InventoryReservation_should_not_be_finished_twice()
    {
        var reservation = new InventoryReservation(
            InventoryReservationId.New(), InventoryItemId.New(), OrderItemId.New(), 1m, new Money(2.50m));
        reservation.Release();

        var action = () => reservation.Consume();

        action.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("stock_reservation.finished");
    }
}
