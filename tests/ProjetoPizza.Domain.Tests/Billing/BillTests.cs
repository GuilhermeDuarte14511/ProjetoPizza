using FluentAssertions;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Billing;

public sealed class BillTests
{
    [Fact]
    public void Request_WithSplitPreference_ShouldPreserveRequestedPeople()
    {
        var bill = CreateBill();

        bill.Request(4);

        bill.Status.Should().Be(BillStatus.Requested);
        bill.RequestedSplitCount.Should().Be(4);
        bill.RequestedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(51)]
    public void Request_WithInvalidSplitPreference_ShouldBeRejected(int people)
    {
        var bill = CreateBill();

        var action = () => bill.Request(people);

        action.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("bill.split_count");
    }

    [Fact]
    public void Request_RepeatedBeforePayment_ShouldUpdatePreferenceWithoutChangingTimestamp()
    {
        var bill = CreateBill();
        bill.Request(2);
        var requestedAt = bill.RequestedAt;

        bill.Request(3);

        bill.RequestedSplitCount.Should().Be(3);
        bill.RequestedAt.Should().Be(requestedAt);
    }

    [Fact]
    public void CounterBill_ShouldBeLinkedToOrderAndApplyDiscount()
    {
        var orderId = OrderId.New();

        var bill = new Bill(
            BillId.New(),
            RestaurantUnitId.New(),
            orderId,
            new Money(100m),
            new Money(10m));

        bill.OrderId.Should().Be(orderId);
        bill.TableSessionId.Should().BeNull();
        bill.TotalAmount.Amount.Should().Be(90m);
        bill.RemainingAmount.Amount.Should().Be(90m);
    }

    [Fact]
    public void CounterBill_WithDiscountAboveSubtotal_ShouldBeRejected()
    {
        var action = () => new Bill(
            BillId.New(),
            RestaurantUnitId.New(),
            OrderId.New(),
            new Money(100m),
            new Money(100.01m));

        action.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("bill.discount");
    }

    private static Bill CreateBill() => new(
        BillId.New(),
        RestaurantUnitId.New(),
        TableSessionId.New(),
        new Money(100m),
        new Percentage(10m));
}
