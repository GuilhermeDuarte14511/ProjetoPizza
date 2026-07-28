using FluentAssertions;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Billing;

public sealed class PaymentTests
{
    [Fact]
    public void Register_WithCash_ShouldCalculateChange()
    {
        var method = CreateMethod(allowsChange: true);

        var payment = new Payment(
            PaymentId.New(), RestaurantUnitId.New(), BillId.New(), method, new Money(80), new Money(100), EmployeeId.New());

        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.ChangeAmount.Should().Be(new Money(20));
    }

    [Fact]
    public void Register_WithZeroAmount_ShouldReject()
    {
        var method = CreateMethod(allowsChange: true);

        var act = () => new Payment(
            PaymentId.New(), RestaurantUnitId.New(), BillId.New(), method, Money.Zero(), Money.Zero(), EmployeeId.New());

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Register_WithChangeOnUnsupportedMethod_ShouldReject()
    {
        var method = CreateMethod(allowsChange: false);

        var act = () => new Payment(
            PaymentId.New(), RestaurantUnitId.New(), BillId.New(), method, new Money(80), new Money(100), EmployeeId.New(), externalReference: "DEV-REF");

        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("payment.change_not_allowed");
    }

    private static PaymentMethod CreateMethod(bool allowsChange) =>
        new(PaymentMethodId.New(), RestaurantUnitId.New(), allowsChange ? "CASH" : "PIX", allowsChange ? "Dinheiro" : "Pix", !allowsChange, allowsChange);
}
