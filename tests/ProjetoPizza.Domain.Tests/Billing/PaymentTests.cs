using FluentAssertions;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Billing;

public sealed class PaymentTests
{
    [Fact]
    public void BillSplit_RegisterPayment_ShouldCloseSplitWhenFullyPaid()
    {
        var split = new BillSplit(BillSplitId.New(), BillId.New(), "Pessoa 1", 1, new Money(33.34m));

        split.RegisterPayment(new Money(33.34m));

        split.Status.Should().Be(BillSplitStatus.Paid);
        split.PaidAmount.Amount.Should().Be(33.34m);
        split.RemainingAmount.Amount.Should().Be(0);
    }

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

    [Fact]
    public void Refund_InParts_ShouldTrackRefundedAmountAndStatus()
    {
        var payment = new Payment(
            PaymentId.New(), RestaurantUnitId.New(), BillId.New(), CreateMethod(allowsChange: true),
            new Money(80), new Money(80), EmployeeId.New());

        payment.Refund(new Money(30), "Cliente desistiu de parte do pedido");
        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        payment.RefundedAmount.Amount.Should().Be(30);

        payment.Refund(new Money(50), "Cancelamento integral");
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAmount.Amount.Should().Be(80);
    }

    [Fact]
    public void Refund_AboveRemainingAmount_ShouldReject()
    {
        var payment = new Payment(
            PaymentId.New(), RestaurantUnitId.New(), BillId.New(), CreateMethod(allowsChange: true),
            new Money(80), new Money(80), EmployeeId.New());

        var act = () => payment.Refund(new Money(80.01m), "Valor incorreto");

        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("payment.invalid_refund");
    }

    private static PaymentMethod CreateMethod(bool allowsChange) =>
        new(PaymentMethodId.New(), RestaurantUnitId.New(), allowsChange ? "CASH" : "PIX", allowsChange ? "Dinheiro" : "Pix", !allowsChange, allowsChange);
}
