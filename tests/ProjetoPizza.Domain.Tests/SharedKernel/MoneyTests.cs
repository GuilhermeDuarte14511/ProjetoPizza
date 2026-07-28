using FluentAssertions;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.SharedKernel;

public sealed class MoneyTests
{
    [Fact]
    public void Create_WithValidValue_ShouldRoundUsingBankersRounding()
    {
        var money = new Money(10.125m);

        money.Amount.Should().Be(10.12m);
        money.Currency.Should().Be(Money.Brl);
    }

    [Fact]
    public void Create_WithNegativeValue_ShouldReject()
    {
        var act = () => new Money(-0.01m);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Add_WithSameCurrency_ShouldSum()
    {
        var result = new Money(10) + new Money(5.50m);

        result.Should().Be(new Money(15.50m));
    }

    [Fact]
    public void Multiply_WithQuantity_ShouldCalculateTotal()
    {
        var result = new Money(12.35m) * 3;

        result.Should().Be(new Money(37.05m));
    }

    [Fact]
    public void Add_WithDifferentCurrencies_ShouldReject()
    {
        var act = () => new Money(10, "BRL") + new Money(10, "USD");

        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("money.currency_mismatch");
    }
}
