using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Application.Client;
using ProjetoPizza.Domain.Inventory;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Application.Inventory;

internal static class InventoryAllocation
{
    public static void Reserve(
        IProjetoPizzaDbContext context,
        Order order,
        IReadOnlyList<SubmitClientOrderItemCommand> requestedItems)
    {
        var orderItems = order.Items.ToArray();
        if (orderItems.Length != requestedItems.Count)
        {
            throw new BusinessRuleException("inventory.order_items", "Order items could not be matched for inventory reservation.");
        }

        var recipes = context.Recipes.ToArray();
        if (recipes.Length == 0) return;
        var recipeItems = context.RecipeItems.ToArray()
            .GroupBy(item => item.RecipeId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var inventoryItems = context.InventoryItems.ToDictionary(item => item.Id);
        var balances = context.StockBalances.ToDictionary(balance => balance.InventoryItemId);
        var planned = Plan(orderItems, requestedItems, recipes, recipeItems)
            .GroupBy(item => new { item.OrderItemId, item.InventoryItemId })
            .Select(group => new PlannedAllocation(
                group.Key.InventoryItemId,
                group.Key.OrderItemId,
                group.Sum(item => item.Quantity)))
            .ToArray();

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

        foreach (var allocation in planned)
        {
            var balance = balances[allocation.InventoryItemId];
            var inventoryItem = inventoryItems[allocation.InventoryItemId];
            balance.Reserve(allocation.Quantity);
            context.Add(new InventoryReservation(
                InventoryReservationId.New(),
                allocation.InventoryItemId,
                allocation.OrderItemId,
                allocation.Quantity,
                inventoryItem.UnitCost));
        }
    }

    public static void Consume(
        IProjetoPizzaDbContext context,
        OrderId orderId,
        long orderNumber,
        EmployeeId employeeId)
    {
        var orderItemIds = context.OrderItems
            .Where(item => item.OrderId == orderId)
            .Select(item => item.Id)
            .ToHashSet();
        var reservations = context.InventoryReservations
            .Where(reservation => orderItemIds.Contains(reservation.OrderItemId) &&
                reservation.Status == InventoryReservationStatus.Reserved)
            .ToArray();
        if (reservations.Length == 0) return;

        var balances = context.StockBalances.ToDictionary(balance => balance.InventoryItemId);
        foreach (var reservation in reservations)
        {
            var balance = balances.GetValueOrDefault(reservation.InventoryItemId)
                ?? throw new BusinessRuleException("stock_balance.missing", "A reserved ingredient does not have a stock balance.");
            balance.ConsumeReserved(reservation.Quantity);
            reservation.Consume();
            context.Add(new StockMovement(
                StockMovementId.New(),
                reservation.InventoryItemId,
                StockMovementType.Consumption,
                reservation.Quantity,
                reservation.UnitCost,
                $"Consumo do pedido #{orderNumber} ao iniciar produção",
                employeeId,
                reservation.OrderItemId));
        }
    }

    public static void Release(IProjetoPizzaDbContext context, OrderId orderId)
    {
        var orderItemIds = context.OrderItems
            .Where(item => item.OrderId == orderId)
            .Select(item => item.Id)
            .ToHashSet();
        var reservations = context.InventoryReservations
            .Where(reservation => orderItemIds.Contains(reservation.OrderItemId) &&
                reservation.Status == InventoryReservationStatus.Reserved)
            .ToArray();
        if (reservations.Length == 0) return;

        var balances = context.StockBalances.ToDictionary(balance => balance.InventoryItemId);
        foreach (var reservation in reservations)
        {
            var balance = balances.GetValueOrDefault(reservation.InventoryItemId)
                ?? throw new BusinessRuleException("stock_balance.missing", "A reserved ingredient does not have a stock balance.");
            balance.ReleaseReserved(reservation.Quantity);
            reservation.Release();
        }
    }

    private static IReadOnlyCollection<PlannedAllocation> Plan(
        IReadOnlyList<OrderItem> orderItems,
        IReadOnlyList<SubmitClientOrderItemCommand> requestedItems,
        IReadOnlyCollection<Recipe> recipes,
        IReadOnlyDictionary<RecipeId, RecipeItem[]> recipeItems)
    {
        var planned = new List<PlannedAllocation>();
        for (var index = 0; index < orderItems.Count; index++)
        {
            var orderItem = orderItems[index];
            var request = requestedItems[index];
            var sizeId = request.Pizza is null ? (PizzaSizeId?)null : new PizzaSizeId(request.Pizza.SizeId);
            foreach (var recipe in recipes.Where(recipe =>
                recipe.ProductId == orderItem.ProductId &&
                recipe.ProductVariantId is null &&
                recipe.PizzaFlavorId is null &&
                (!recipe.PizzaSizeId.HasValue || recipe.PizzaSizeId == sizeId)))
            {
                AddRecipe(planned, recipe, recipeItems, orderItem, orderItem.Quantity);
            }

            var flavorIds = request.Pizza?.FlavorIds?.ToArray() ?? [];
            foreach (var flavorId in flavorIds)
            {
                foreach (var recipe in recipes.Where(recipe =>
                    recipe.PizzaFlavorId == new PizzaFlavorId(flavorId) &&
                    (!recipe.PizzaSizeId.HasValue || recipe.PizzaSizeId == sizeId)))
                {
                    AddRecipe(planned, recipe, recipeItems, orderItem, orderItem.Quantity / (decimal)flavorIds.Length);
                }
            }
        }

        return planned;
    }

    private static void AddRecipe(
        ICollection<PlannedAllocation> planned,
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
            planned.Add(new PlannedAllocation(ingredient.InventoryItemId, orderItem.Id, quantity));
        }
    }

    private sealed record PlannedAllocation(
        InventoryItemId InventoryItemId,
        OrderItemId OrderItemId,
        decimal Quantity);
}
