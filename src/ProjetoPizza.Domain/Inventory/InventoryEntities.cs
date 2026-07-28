using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Inventory;

public enum StockMovementType
{
    Entry,
    Consumption,
    Adjustment,
    Loss,
    Return,
    Reservation,
    ReservationRelease
}

public sealed class InventoryItem : AggregateRoot<InventoryItemId>
{
    private InventoryItem() : base(default) { }

    public InventoryItem(InventoryItemId id, RestaurantUnitId unitId, string name, string sku, string unitOfMeasure, decimal minimumStock) : base(id)
    {
        UnitId = unitId;
        Name = Guard.Required(name, nameof(name), 120);
        Sku = Guard.Required(sku, nameof(sku), 50);
        UnitOfMeasure = Guard.Required(unitOfMeasure, nameof(unitOfMeasure), 20);
        MinimumStock = Guard.NonNegative(minimumStock, nameof(minimumStock));
        IsActive = true;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal MinimumStock { get; private set; }
    public bool IsActive { get; private set; }
}

public sealed class StockBalance : AggregateRoot<StockBalanceId>
{
    private StockBalance() : base(default) { }

    public StockBalance(StockBalanceId id, InventoryItemId inventoryItemId) : base(id)
    {
        InventoryItemId = inventoryItemId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public InventoryItemId InventoryItemId { get; private set; }
    public decimal CurrentQuantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public decimal AvailableQuantity => CurrentQuantity - ReservedQuantity;
}

public sealed class StockMovement : Entity<StockMovementId>
{
    private StockMovement() : base(default) { }

    public StockMovement(
        StockMovementId id,
        InventoryItemId inventoryItemId,
        StockMovementType movementType,
        decimal quantity,
        Money unitCost,
        string reason,
        EmployeeId createdByEmployeeId) : base(id)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("stock_movement.quantity", "Stock movement quantity must be greater than zero.");
        }

        InventoryItemId = inventoryItemId;
        MovementType = movementType;
        Quantity = quantity;
        UnitCost = unitCost;
        Reason = Guard.Required(reason, nameof(reason), 300);
        CreatedByEmployeeId = createdByEmployeeId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public InventoryItemId InventoryItemId { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public decimal Quantity { get; private set; }
    public Money UnitCost { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public OrderItemId? OrderItemId { get; private set; }
    public EmployeeId CreatedByEmployeeId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

public sealed class Recipe : AggregateRoot<RecipeId>
{
    private Recipe() : base(default) { }

    public Recipe(RecipeId id, decimal yieldQuantity, ProductId? productId = null, ProductVariantId? productVariantId = null, PizzaFlavorId? pizzaFlavorId = null, PizzaSizeId? pizzaSizeId = null) : base(id)
    {
        if (productId is null && productVariantId is null && pizzaFlavorId is null)
        {
            throw new BusinessRuleException("recipe.target", "A recipe needs a product, variant, or pizza flavor.");
        }

        if (yieldQuantity <= 0)
        {
            throw new BusinessRuleException("recipe.yield", "Recipe yield must be greater than zero.");
        }

        ProductId = productId;
        ProductVariantId = productVariantId;
        PizzaFlavorId = pizzaFlavorId;
        PizzaSizeId = pizzaSizeId;
        YieldQuantity = yieldQuantity;
    }

    public ProductId? ProductId { get; private set; }
    public ProductVariantId? ProductVariantId { get; private set; }
    public PizzaFlavorId? PizzaFlavorId { get; private set; }
    public PizzaSizeId? PizzaSizeId { get; private set; }
    public decimal YieldQuantity { get; private set; }
}

public sealed class RecipeItem : Entity<RecipeItemId>
{
    private RecipeItem() : base(default) { }

    public RecipeItem(RecipeItemId id, RecipeId recipeId, InventoryItemId inventoryItemId, decimal quantity, string unitOfMeasure) : base(id)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("recipe_item.quantity", "Recipe item quantity must be greater than zero.");
        }

        RecipeId = recipeId;
        InventoryItemId = inventoryItemId;
        Quantity = quantity;
        UnitOfMeasure = Guard.Required(unitOfMeasure, nameof(unitOfMeasure), 20);
    }

    public RecipeId RecipeId { get; private set; }
    public InventoryItemId InventoryItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
}
