using FluentAssertions;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Cashier;

public sealed class CashShiftTests
{
    [Fact]
    public void Open_ShouldPreserveOpeningAmount()
    {
        var shift = CreateShift();

        shift.Status.Should().Be(CashShiftStatus.Open);
        shift.ExpectedCashAmount.Should().Be(new Money(100));
    }

    [Fact]
    public void RegisterMovement_ShouldUpdateExpectedCash()
    {
        var shift = CreateShift();

        shift.RegisterMovement(CashMovementId.New(), CashMovementType.Sale, new Money(50), "Venda", "Pedido 1", EmployeeId.New());

        shift.Movements.Should().ContainSingle();
        shift.ExpectedCashAmount.Should().Be(new Money(150));
    }

    [Fact]
    public void Close_ShouldCalculateDifferenceAndPreventSecondClose()
    {
        var shift = CreateShift();
        var employeeId = EmployeeId.New();
        shift.Close(employeeId, new Money(98));

        var act = () => shift.Close(employeeId, new Money(98));

        shift.DifferenceAmount.Should().Be(-2);
        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("cash_shift.already_closed");
    }

    private static CashShift CreateShift() =>
        new(CashShiftId.New(), CashRegisterId.New(), EmployeeId.New(), new Money(100));
}
