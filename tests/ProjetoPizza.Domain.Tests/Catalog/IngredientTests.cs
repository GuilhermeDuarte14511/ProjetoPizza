using FluentAssertions;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Catalog;

public sealed class IngredientTests
{
    [Fact]
    public void Update_ShouldConfigureIngredientAsPricedExtra()
    {
        var ingredient = new Ingredient(
            IngredientId.New(),
            RestaurantUnitId.New(),
            "Bacon");

        ingredient.Update(
            "Bacon",
            "Bacon crocante em cubos.",
            isActive: true,
            isAllergen: false,
            allergenDescription: null,
            isAvailableAsExtra: true,
            new Money(8m),
            maxExtraQuantity: 3);

        ingredient.IsAvailableAsExtra.Should().BeTrue();
        ingredient.ExtraPrice.Amount.Should().Be(8m);
        ingredient.MaxExtraQuantity.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Update_WithInvalidExtraLimit_ShouldBeRejected(int limit)
    {
        var ingredient = new Ingredient(
            IngredientId.New(),
            RestaurantUnitId.New(),
            "Bacon");

        var action = () => ingredient.Update(
            "Bacon",
            null,
            true,
            false,
            null,
            true,
            new Money(8m),
            limit);

        action.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("ingredient.max_extra_quantity");
    }

    [Fact]
    public void PizzaFlavorExtra_ShouldKeepFlavorSpecificPriceAndLimit()
    {
        var extra = new PizzaFlavorExtra(
            PizzaFlavorId.New(),
            IngredientId.New(),
            new Money(6.5m),
            2);

        extra.Update(new Money(9m), 3, isActive: true);

        extra.Price.Amount.Should().Be(9m);
        extra.MaxQuantity.Should().Be(3);
        extra.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void PizzaFlavorExtra_WithInvalidLimit_ShouldBeRejected(int limit)
    {
        var action = () => new PizzaFlavorExtra(
            PizzaFlavorId.New(),
            IngredientId.New(),
            new Money(6.5m),
            limit);

        action.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("pizza_flavor_extra.max_quantity");
    }

    [Fact]
    public void ProductExtra_ShouldKeepProductSpecificPriceAndLimit()
    {
        var extra = new ProductExtra(
            ProductId.New(),
            IngredientId.New(),
            new Money(7m),
            2);

        extra.Update(new Money(8.5m), 3, isActive: true);

        extra.Price.Amount.Should().Be(8.5m);
        extra.MaxQuantity.Should().Be(3);
        extra.IsActive.Should().BeTrue();
    }
}
