using FluentAssertions;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Ordering;

public sealed class OrderTests
{
    [Fact]
    public void AddItem_WithValidData_ShouldRecalculateTotal()
    {
        var order = CreateOrder();

        order.AddItem(OrderItemId.New(), ProductId.New(), "Pizza", 2, new Money(30));

        order.Items.Should().ContainSingle();
        order.Subtotal.Should().Be(new Money(60));
        order.Total.Should().Be(new Money(60));
    }

    [Fact]
    public void AddItem_WithInvalidQuantity_ShouldReject()
    {
        var order = CreateOrder();

        var act = () => order.AddItem(OrderItemId.New(), ProductId.New(), "Pizza", 0, new Money(30));

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Cancel_ShouldPreventChanges()
    {
        var order = CreateOrder();
        order.AddItem(OrderItemId.New(), ProductId.New(), "Pizza", 1, new Money(30));
        order.Cancel("Cliente desistiu.");

        var act = () => order.AddItem(OrderItemId.New(), ProductId.New(), "Bebida", 1, new Money(10));

        order.Status.Should().Be(OrderStatus.Cancelled);
        act.Should().Throw<BusinessRuleException>();
    }

    private static Order CreateOrder() =>
        new(OrderId.New(), RestaurantUnitId.New(), 1, SalesChannel.DineIn, FulfillmentType.DineIn);
}
