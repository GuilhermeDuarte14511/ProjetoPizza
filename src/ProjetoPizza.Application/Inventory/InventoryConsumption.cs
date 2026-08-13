using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Application.Client;
using ProjetoPizza.Domain.Inventory;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Application.Inventory;

internal static class InventoryConsumption
{
    public static void Apply(
        IProjetoPizzaDbContext context,
        Order order,
        IReadOnlyList<SubmitClientOrderItemCommand> requestedItems,
        EmployeeId employeeId)
    {
        var orderItems = order.Items.ToArray();
        if (orderItems.Length != requestedItems.Count)
        {
            throw new BusinessRuleException("inventory.order_items", "Order items could not be matched for inventory consumption.");
        }

        var recipes = context.Recipes.ToArray();
        if (recipes.Length == 0) return;
        var recipeItems = context.RecipeItems.ToArray()
            .GroupBy(item => item.RecipeId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var inventoryItems = context.InventoryItems.ToDictionary(item => item.Id);
        var balances = context.StockBalances.ToDictionary(balance => balance.InventoryItemId);
        var planned = new List<PlannedConsumption>();

        for (var index = 0; index < orderItems.Length; index++)
        {
            var orderItem = orderItems[index];
            var request = requestedItems[index];
            var sizeId = request.Pizza is null ? (PizzaSizeId?)null : new PizzaSizeId(request.Pizza.SizeId);
            var applicableRecipes = recipes.Where(recipe =>
                recipe.ProductId == orderItem.ProductId &&
                recipe.ProductVariantId is null &&
                recipe.PizzaFlavorId is null &&
                (!recipe.PizzaSizeId.HasValue || recipe.PizzaSizeId == sizeId));
            foreach (var recipe in applicableRecipes)
            {
                AddRecipe(planned, recipe, recipeItems, orderItem, orderItem.Quantity);
            }

            var flavorIds = request.Pizza?.FlavorIds?.ToArray() ?? [];
            foreach (var flavorId in flavorIds)
            {
                var flavorRecipes = recipes.Where(recipe =>
                    recipe.PizzaFlavorId == new PizzaFlavorId(flavorId) &&
                    (!recipe.PizzaSizeId.HasValue || recipe.PizzaSizeId == sizeId));
                foreach (var recipe in flavorRecipes)
                {
                    AddRecipe(planned, recipe, recipeItems, orderItem, orderItem.Quantity / (decimal)flavorIds.Length);
                }
            }
        }

        foreach (var group in planned.GroupBy(item => item.InventoryItemId))
        {
            if (!balances.TryGetValue(group.Key, out var balance))
            {
                throw new BusinessRuleException("stock_balance.missing", "A recipe ingredient does not have a stock balance.");
            }
            var required = group.Sum(item => item.Quantity);
            if (balance.AvailableQuantity < required)
            {
                var name = inventoryItems.GetValueOrDefault(group.Key)?.Name ?? "ingrediente";
                throw new BusinessRuleException("stock_balance.insufficient", $"Insufficient stock for {name}.");
            }
        }

        foreach (var consumption in planned)
        {
            var balance = balances[consumption.InventoryItemId];
            var inventoryItem = inventoryItems[consumption.InventoryItemId];
            balance.ApplyAdjustment(-consumption.Quantity);
            context.Add(new StockMovement(
                StockMovementId.New(),
                consumption.InventoryItemId,
                StockMovementType.Consumption,
                consumption.Quantity,
                inventoryItem.UnitCost,
                $"Consumo automático do pedido #{order.OrderNumber}",
                employeeId,
                consumption.OrderItemId));
        }
    }

    private static void AddRecipe(
        ICollection<PlannedConsumption> planned,
        Recipe recipe,
        IReadOnlyDictionary<RecipeId, RecipeItem[]> recipeItems,
        OrderItem orderItem,
        decimal producedQuantity)
    {
        foreach (var ingredient in recipeItems.GetValueOrDefault(recipe.Id, []))
        {
            var quantity = decimal.Round(
                ingredient.Quantity / recipe.YieldQuantity * producedQuantity,
                4,
                MidpointRounding.AwayFromZero);
            if (quantity <= 0) continue;

            planned.Add(new PlannedConsumption(
                ingredient.InventoryItemId,
                orderItem.Id,
                quantity));
        }
    }

    private sealed record PlannedConsumption(InventoryItemId InventoryItemId, OrderItemId OrderItemId, decimal Quantity);
}
