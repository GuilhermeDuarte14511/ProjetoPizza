using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Catalog;

public enum ProductType
{
    Standard,
    Pizza,
    PizzaFlavor,
    Beverage,
    Portion,
    Dessert,
    Combo,
    Additional
}

public enum PizzaFlavorType
{
    Savory,
    Sweet
}

public sealed class Category : AggregateRoot<CategoryId>
{
    private Category() : base(default) { }

    public Category(CategoryId id, RestaurantUnitId unitId, string name, string slug, int displayOrder = 0) : base(id)
    {
        UnitId = unitId;
        Name = Guard.Required(name, nameof(name), 100);
        Slug = Guard.Required(slug, nameof(slug), 120);
        DisplayOrder = (int)Guard.NonNegative(displayOrder, nameof(displayOrder));
        IsVisibleOnTablet = IsActive = true;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public CategoryId? ParentCategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsVisibleOnTablet { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string slug, string? description, bool isVisibleOnTablet, bool isActive)
    {
        Name = Guard.Required(name, nameof(name), 100);
        Slug = Guard.Required(slug, nameof(slug), 120);
        Description = string.IsNullOrWhiteSpace(description) ? null : Guard.Required(description, nameof(description), 500);
        IsVisibleOnTablet = isVisibleOnTablet;
        IsActive = isActive;
    }
}

public sealed class Product : AggregateRoot<ProductId>
{
    private Product() : base(default) { }

    public Product(
        ProductId id,
        RestaurantUnitId unitId,
        CategoryId categoryId,
        string sku,
        string name,
        ProductType productType,
        Money basePrice) : base(id)
    {
        UnitId = unitId;
        CategoryId = categoryId;
        Sku = Guard.Required(sku, nameof(sku), 50);
        Name = Guard.Required(name, nameof(name), 140);
        ProductType = productType;
        BasePrice = basePrice;
        IsActive = IsAvailable = true;
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProductType ProductType { get; private set; }
    public Money BasePrice { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsPopular { get; private set; }
    public int PreparationTimeMinutes { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Activate() => ChangeActive(true);
    public void Deactivate() => ChangeActive(false);
    public void MakeAvailable() => ChangeAvailability(true);
    public void MakeUnavailable() => ChangeAvailability(false);
    public void MarkAsFeatured() => SetFeatured(true);
    public void RemoveFromFeatured() => SetFeatured(false);
    public void SetActive(bool value) => ChangeActive(value);
    public void SetAvailable(bool value) => ChangeAvailability(value);

    public void ChangePrice(Money price)
    {
        BasePrice = price;
        Touch();
    }

    public void ChangeCategory(CategoryId categoryId)
    {
        CategoryId = categoryId;
        Touch();
    }

    public void UpdateInformation(string name, string? description, int preparationTimeMinutes)
    {
        Name = Guard.Required(name, nameof(name), 140);
        Description = string.IsNullOrWhiteSpace(description) ? null : Guard.Required(description, nameof(description), 1000);
        PreparationTimeMinutes = (int)Guard.NonNegative(preparationTimeMinutes, nameof(preparationTimeMinutes));
        Touch();
    }

    private void ChangeActive(bool value) { IsActive = value; Touch(); }
    private void ChangeAvailability(bool value) { IsAvailable = value; Touch(); }
    private void SetFeatured(bool value) { IsFeatured = value; Touch(); }
    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

public sealed class ProductVariant : Entity<ProductVariantId>
{
    private ProductVariant() : base(default) { }

    public ProductVariant(ProductVariantId id, ProductId productId, string name, string sku, Money price) : base(id)
    {
        ProductId = productId;
        Name = Guard.Required(name, nameof(name), 100);
        Sku = Guard.Required(sku, nameof(sku), 50);
        Price = price;
        IsActive = IsAvailable = true;
    }

    public ProductId ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public Money Price { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsAvailable { get; private set; }
    public int DisplayOrder { get; private set; }
}

public sealed class ProductImage : Entity<ProductImageId>
{
    private ProductImage() : base(default) { }

    public ProductImage(ProductImageId id, ProductId productId, string url, string altText, int displayOrder = 0) : base(id)
    {
        ProductId = productId;
        Url = Guard.Required(url, nameof(url), 1000);
        AltText = Guard.Required(altText, nameof(altText), 160);
        DisplayOrder = (int)Guard.NonNegative(displayOrder, nameof(displayOrder));
    }

    public ProductId ProductId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string AltText { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
}

public sealed class PizzaSize : AggregateRoot<PizzaSizeId>
{
    private PizzaSize() : base(default) { }

    public PizzaSize(
        PizzaSizeId id,
        RestaurantUnitId unitId,
        string name,
        string shortName,
        int slices,
        decimal diameterCm,
        Money basePrice,
        int maxFlavors,
        int displayOrder = 0) : base(id)
    {
        UnitId = unitId;
        Name = Guard.Required(name, nameof(name), 80);
        ShortName = Guard.Required(shortName, nameof(shortName), 12);
        Slices = Guard.Positive(slices, nameof(slices));
        if (diameterCm <= 0)
        {
            throw new BusinessRuleException("pizza_size.diameter", "Diameter must be greater than zero.");
        }

        if (maxFlavors is < 1 or > 3)
        {
            throw new BusinessRuleException("pizza_size.max_flavors", "Pizza size supports between one and three flavors.");
        }

        DiameterCm = diameterCm;
        BasePrice = basePrice;
        MaxFlavors = maxFlavors;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ShortName { get; private set; } = string.Empty;
    public int Slices { get; private set; }
    public decimal DiameterCm { get; private set; }
    public Money BasePrice { get; private set; }
    public int MaxFlavors { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string name,
        string shortName,
        int slices,
        decimal diameterCm,
        Money basePrice,
        int maxFlavors,
        bool isActive)
    {
        if (diameterCm <= 0)
        {
            throw new BusinessRuleException("pizza_size.diameter", "Diameter must be greater than zero.");
        }

        if (maxFlavors is < 1 or > 3)
        {
            throw new BusinessRuleException("pizza_size.max_flavors", "Pizza size supports between one and three flavors.");
        }

        Name = Guard.Required(name, nameof(name), 80);
        ShortName = Guard.Required(shortName, nameof(shortName), 12);
        Slices = Guard.Positive(slices, nameof(slices));
        DiameterCm = diameterCm;
        BasePrice = basePrice;
        MaxFlavors = maxFlavors;
        IsActive = isActive;
    }
}

public sealed class PizzaFlavor : AggregateRoot<PizzaFlavorId>
{
    private PizzaFlavor() : base(default) { }

    public PizzaFlavor(
        PizzaFlavorId id,
        RestaurantUnitId unitId,
        CategoryId categoryId,
        string name,
        PizzaFlavorType flavorType) : base(id)
    {
        UnitId = unitId;
        CategoryId = categoryId;
        Name = Guard.Required(name, nameof(name), 120);
        FlavorType = flavorType;
        IsActive = IsAvailable = true;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PizzaFlavorType FlavorType { get; private set; }
    public bool IsPremium { get; private set; }
    public bool IsVegetarian { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsAvailable { get; private set; }
    public string? SoldOutReason { get; private set; }
    public string? ImageUrl { get; private set; }
    public int DisplayOrder { get; private set; }

    public void MarkSoldOut(string reason)
    {
        SoldOutReason = Guard.Required(reason, nameof(reason), 300);
        IsAvailable = false;
    }

    public void MakeAvailable()
    {
        SoldOutReason = null;
        IsAvailable = true;
    }

    public void Update(
        string name,
        string? description,
        PizzaFlavorType flavorType,
        bool isPremium,
        bool isVegetarian,
        bool isActive,
        bool isAvailable,
        string? soldOutReason)
    {
        Name = Guard.Required(name, nameof(name), 120);
        Description = description;
        FlavorType = flavorType;
        IsPremium = isPremium;
        IsVegetarian = isVegetarian;
        IsActive = isActive;
        if (isAvailable)
        {
            MakeAvailable();
        }
        else
        {
            MarkSoldOut(string.IsNullOrWhiteSpace(soldOutReason) ? "Indisponível pelo painel administrativo." : soldOutReason);
        }
    }
}

public sealed class PizzaFlavorPrice : Entity<PizzaFlavorPriceId>
{
    private PizzaFlavorPrice() : base(default) { }

    public PizzaFlavorPrice(PizzaFlavorPriceId id, PizzaFlavorId pizzaFlavorId, PizzaSizeId pizzaSizeId, Money price, Money additionalPrice) : base(id)
    {
        PizzaFlavorId = pizzaFlavorId;
        PizzaSizeId = pizzaSizeId;
        Price = price;
        AdditionalPrice = additionalPrice;
        IsAvailable = true;
    }

    public PizzaFlavorId PizzaFlavorId { get; private set; }
    public PizzaSizeId PizzaSizeId { get; private set; }
    public Money Price { get; private set; }
    public Money AdditionalPrice { get; private set; }
    public bool IsAvailable { get; private set; }
}

public sealed class PizzaCrust : AggregateRoot<PizzaCrustId>
{
    private PizzaCrust() : base(default) { }

    public PizzaCrust(PizzaCrustId id, RestaurantUnitId unitId, string name, string? description = null) : base(id)
    {
        UnitId = unitId;
        Name = Guard.Required(name, nameof(name), 100);
        Description = description;
        IsActive = IsAvailable = true;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsAvailable { get; private set; }
    public int DisplayOrder { get; private set; }

    public void Update(string name, string? description, bool isActive, bool isAvailable)
    {
        Name = Guard.Required(name, nameof(name), 100);
        Description = string.IsNullOrWhiteSpace(description) ? null : Guard.Required(description, nameof(description), 500);
        IsActive = isActive;
        IsAvailable = isAvailable;
    }
}

public sealed class PizzaCrustPrice : Entity<PizzaCrustPriceId>
{
    private PizzaCrustPrice() : base(default) { }

    public PizzaCrustPrice(PizzaCrustPriceId id, PizzaCrustId pizzaCrustId, PizzaSizeId pizzaSizeId, Money additionalPrice) : base(id)
    {
        PizzaCrustId = pizzaCrustId;
        PizzaSizeId = pizzaSizeId;
        AdditionalPrice = additionalPrice;
    }

    public PizzaCrustId PizzaCrustId { get; private set; }
    public PizzaSizeId PizzaSizeId { get; private set; }
    public Money AdditionalPrice { get; private set; }
}

public sealed class Ingredient : AggregateRoot<IngredientId>
{
    private Ingredient() : base(default) { }

    public Ingredient(IngredientId id, RestaurantUnitId unitId, string name, InventoryItemId? inventoryItemId = null) : base(id)
    {
        UnitId = unitId;
        Name = Guard.Required(name, nameof(name), 120);
        InventoryItemId = inventoryItemId;
        IsActive = true;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public InventoryItemId? InventoryItemId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsAllergen { get; private set; }
    public string? AllergenDescription { get; private set; }
}

public sealed class PizzaFlavorIngredient
{
    private PizzaFlavorIngredient() { }

    public PizzaFlavorIngredient(PizzaFlavorId pizzaFlavorId, IngredientId ingredientId, decimal quantity, string unitOfMeasure, int displayOrder = 0)
    {
        PizzaFlavorId = pizzaFlavorId;
        IngredientId = ingredientId;
        if (quantity <= 0)
        {
            throw new BusinessRuleException("ingredient.quantity", "Ingredient quantity must be greater than zero.");
        }

        Quantity = quantity;
        UnitOfMeasure = Guard.Required(unitOfMeasure, nameof(unitOfMeasure), 20);
        DisplayOrder = displayOrder;
        IsRemovable = IsDefault = true;
    }

    public PizzaFlavorId PizzaFlavorId { get; private set; }
    public IngredientId IngredientId { get; private set; }
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public bool IsRemovable { get; private set; }
    public bool IsDefault { get; private set; }
    public int DisplayOrder { get; private set; }
}
