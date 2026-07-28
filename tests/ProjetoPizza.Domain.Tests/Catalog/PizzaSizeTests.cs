using FluentAssertions;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Catalog;

public sealed class PizzaSizeTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Create_WithSupportedFlavorLimit_ShouldAccept(int maxFlavors)
    {
        var size = Create(maxFlavors);

        size.MaxFlavors.Should().Be(maxFlavors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Create_WithUnsupportedFlavorLimit_ShouldReject(int maxFlavors)
    {
        var act = () => Create(maxFlavors);

        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("pizza_size.max_flavors");
    }

    private static PizzaSize Create(int maxFlavors) =>
        new(PizzaSizeId.New(), RestaurantUnitId.New(), "Grande", "G", 8, 35, new Money(68), maxFlavors);
}
