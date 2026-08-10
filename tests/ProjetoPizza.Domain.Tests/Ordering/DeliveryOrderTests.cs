using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Ordering;

public sealed class DeliveryOrderTests
{
    [Fact]
    public void Delivery_requires_dispatch_before_completion()
    {
        var order = CreateDeliveryOrder();
        order.Accept();
        order.StartProduction();
        order.MarkReady();

        Assert.Equal(DeliveryStatus.ReadyForDispatch, order.DeliveryStatus);
        Assert.Throws<BusinessRuleException>(() => order.CompleteDelivery());

        order.DispatchDelivery("João Entregador");
        order.CompleteDelivery();

        Assert.Equal(DeliveryStatus.Delivered, order.DeliveryStatus);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.NotNull(order.DispatchedAt);
        Assert.NotNull(order.DeliveredAt);
    }

    private static Order CreateDeliveryOrder()
    {
        var order = new Order(
            OrderId.New(), RestaurantUnitId.New(), 101,
            SalesChannel.Website, FulfillmentType.Delivery);
        order.AssignCustomer(CustomerId.New(), "Cliente Teste");
        order.ConfigureDeliveryAddress("Rua de Teste, 100");
        order.ConfigureDeliveryTracking(new string('A', 64));
        order.AddItem(OrderItemId.New(), ProductId.New(), "Pizza", 1, new Money(45));
        order.Submit();
        return order;
    }
}
