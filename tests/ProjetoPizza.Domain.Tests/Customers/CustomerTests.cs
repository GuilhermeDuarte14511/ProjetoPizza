using FluentAssertions;
using ProjetoPizza.Domain.Customers;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Customers;

public sealed class CustomerTests
{
    [Fact]
    public void Purchase_should_accumulate_and_cancellation_should_reverse_customer_statistics()
    {
        var customer = new Customer(
            CustomerId.New(), RestaurantUnitId.New(), "Ana Souza", "11999998877", new DateOnly(1992, 5, 18));

        customer.RegisterPurchase(new Money(72.90m));
        customer.ReversePurchase(new Money(72.90m));

        customer.LoyaltyPoints.Should().Be(0);
        customer.LifetimeSpend.Should().Be(Money.Zero());
        customer.OrderCount.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldNormalizePhoneAndKeepProfile()
    {
        var birthDate = new DateOnly(1992, 5, 18);

        var customer = new Customer(
            CustomerId.New(),
            RestaurantUnitId.New(),
            "Ana Souza",
            "(11) 99999-8877",
            birthDate);

        customer.Name.Should().Be("Ana Souza");
        customer.Phone.Should().Be("11999998877");
        customer.BirthDate.Should().Be(birthDate);
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithFutureBirthDate_ShouldReject()
    {
        var act = () => new Customer(
            CustomerId.New(),
            RestaurantUnitId.New(),
            "Ana Souza",
            "11999998877",
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("customer.birth_date");
    }

    [Fact]
    public void AdjustLoyaltyPoints_ShouldCreditAndDebitWithoutAllowingNegativeBalance()
    {
        var customer = new Customer(
            CustomerId.New(), RestaurantUnitId.New(), "Ana Souza", "11999998877", new DateOnly(1992, 5, 18));
        var expiration = DateTimeOffset.UtcNow.AddDays(90);

        customer.AdjustLoyaltyPoints(120, expiration);
        customer.AdjustLoyaltyPoints(-35, expiration);

        customer.LoyaltyPoints.Should().Be(85);
        customer.LoyaltyPointsExpireAt.Should().Be(expiration);
        var action = () => customer.AdjustLoyaltyPoints(-86, expiration);
        action.Should().Throw<BusinessRuleException>().Which.Rule.Should().Be("loyalty.balance");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000001)]
    [InlineData(-1000001)]
    public void AdjustLoyaltyPoints_WithUnsupportedAmount_ShouldReject(int points)
    {
        var customer = new Customer(
            CustomerId.New(), RestaurantUnitId.New(), "Ana Souza", "11999998877", new DateOnly(1992, 5, 18));

        var action = () => customer.AdjustLoyaltyPoints(points, DateTimeOffset.UtcNow.AddDays(90));

        action.Should().Throw<BusinessRuleException>().Which.Rule.Should().Be("loyalty.adjustment");
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("1234567890123456")]
    public void Create_WithInvalidPhone_ShouldReject(string phone)
    {
        var act = () => new Customer(
            CustomerId.New(),
            RestaurantUnitId.New(),
            "Ana Souza",
            phone,
            new DateOnly(1992, 5, 18));

        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("customer.phone");
    }
}
