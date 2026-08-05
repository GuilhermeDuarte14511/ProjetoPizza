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

    [Fact]
    public void Submit_PickupWithoutCustomer_ShouldReject()
    {
        var order = new Order(
            OrderId.New(),
            RestaurantUnitId.New(),
            2,
            SalesChannel.Pickup,
            FulfillmentType.Pickup);
        order.AddItem(OrderItemId.New(), ProductId.New(), "Pizza", 1, new Money(30));

        var act = order.Submit;

        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("order.customer_required");
    }

    [Fact]
    public void Submit_DeliveryWithoutAddress_ShouldReject()
    {
        var order = new Order(
            OrderId.New(),
            RestaurantUnitId.New(),
            3,
            SalesChannel.Delivery,
            FulfillmentType.Delivery);
        order.AssignCustomer(CustomerId.New(), "Maria da Silva");
        order.AddItem(OrderItemId.New(), ProductId.New(), "Pizza", 1, new Money(40));

        var act = order.Submit;

        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("order.delivery_address_required");
    }

    [Fact]
    public void Submit_DeliveryWithCustomerAddressFeeAndDiscount_ShouldCalculateTotal()
    {
        var order = new Order(
            OrderId.New(),
            RestaurantUnitId.New(),
            4,
            SalesChannel.Delivery,
            FulfillmentType.Delivery);
        order.AssignCustomer(CustomerId.New(), "Maria da Silva");
        order.ConfigureDeliveryAddress("Rua das Flores, 27 - Centro");
        order.SetNotes("Entregar na portaria.");
        order.AddItem(OrderItemId.New(), ProductId.New(), "Pizza grande", 2, new Money(40));

        order.RecalculateTotals(deliveryFee: new Money(8), discount: new Money(10));
        order.Submit();

        order.Subtotal.Should().Be(new Money(80));
        order.DeliveryFee.Should().Be(new Money(8));
        order.Discount.Should().Be(new Money(10));
        order.Total.Should().Be(new Money(78));
        order.Status.Should().Be(OrderStatus.Submitted);
        order.Notes.Should().Be("Entregar na portaria.");
    }

    private static Order CreateOrder() =>
        new(OrderId.New(), RestaurantUnitId.New(), 1, SalesChannel.DineIn, FulfillmentType.DineIn);
}
