using FluentAssertions;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Ordering;

public sealed class PizzaCompositionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AddFlavor_WithinSizeLimit_ShouldCreateValidPizza(int flavorCount)
    {
        var pizza = CreatePizza(3);

        for (var index = 0; index < flavorCount; index++)
        {
            pizza.AddFlavor(OrderItemPizzaFlavorId.New(), PizzaFlavorId.New(), $"Sabor {index}", new Money(10));
        }

        pizza.EnsureValidComposition();
        pizza.FlavorCount.Should().Be(flavorCount);
        pizza.Flavors.Should().OnlyContain(flavor => flavor.TotalParts == flavorCount);
    }

    [Fact]
    public void AddFourthFlavor_ShouldReject()
    {
        var pizza = CreatePizza(3);
        AddUniqueFlavors(pizza, 3);

        var act = () => pizza.AddFlavor(OrderItemPizzaFlavorId.New(), PizzaFlavorId.New(), "Quarto", new Money(10));

        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("pizza.flavor_limit");
    }

    [Fact]
    public void AddRepeatedFlavor_WhenNotAllowed_ShouldReject()
    {
        var pizza = CreatePizza(3);
        var flavorId = PizzaFlavorId.New();
        pizza.AddFlavor(OrderItemPizzaFlavorId.New(), flavorId, "Calabresa", new Money(10));

        var act = () => pizza.AddFlavor(OrderItemPizzaFlavorId.New(), flavorId, "Calabresa", new Money(10));

        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("pizza.repeated_flavor");
    }

    [Fact]
    public void AddFlavor_AboveSizeLimit_ShouldReject()
    {
        var pizza = CreatePizza(2);
        AddUniqueFlavors(pizza, 2);

        var act = () => pizza.AddFlavor(OrderItemPizzaFlavorId.New(), PizzaFlavorId.New(), "Terceiro", new Money(10));

        act.Should().Throw<BusinessRuleException>();
    }

    private static OrderItemPizza CreatePizza(int maxFlavors) =>
        new(OrderItemId.New(), PizzaSizeId.New(), "Grande", 8, maxFlavors, PizzaPricingPolicy.HighestFlavorPrice, new Money(68));

    private static void AddUniqueFlavors(OrderItemPizza pizza, int count)
    {
        for (var index = 0; index < count; index++)
        {
            pizza.AddFlavor(OrderItemPizzaFlavorId.New(), PizzaFlavorId.New(), $"Sabor {index}", new Money(10));
        }
    }
}
