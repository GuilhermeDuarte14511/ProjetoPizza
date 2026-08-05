using FluentAssertions;
using ProjetoPizza.Domain.Customers;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Customers;

public sealed class CustomerTests
{
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
