using FluentAssertions;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Core;

public sealed class PizzaPricingPoliciesTests
{
    [Theory]
    [InlineData(PizzaPricingPolicy.HighestFlavorPrice, 60)]
    [InlineData(PizzaPricingPolicy.AverageFlavorPrice, 50)]
    [InlineData(PizzaPricingPolicy.ProportionalFlavorPrice, 50)]
    public void Calculate_ShouldApplyConfiguredPolicy(PizzaPricingPolicy policy, decimal expected)
    {
        var result = PizzaPricingPolicies.Calculate(
            policy,
            [new Money(40m), new Money(60m)]);

        result.Amount.Should().Be(expected);
    }

    [Fact]
    public void Calculate_WithoutFlavors_ShouldBeRejected()
    {
        var action = () => PizzaPricingPolicies.Calculate(
            PizzaPricingPolicy.HighestFlavorPrice,
            []);

        action.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("pizza_pricing.flavors_required");
    }
}
