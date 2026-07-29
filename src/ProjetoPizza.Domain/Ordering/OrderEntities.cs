using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Ordering;

public enum OrderStatus
{
    Draft,
    Submitted,
    Accepted,
    InProduction,
    Ready,
    Completed,
    Cancelled
}

public enum OrderPaymentStatus
{
    Pending,
    PartiallyPaid,
    Paid,
    Refunded
}

public enum SalesChannel
{
    DineIn,
    Delivery,
    Pickup,
    Website,
    Application,
    Administrative
}

public enum FulfillmentType
{
    DineIn,
    Delivery,
    Pickup
}

public enum OrderItemStatus
{
    Pending,
    SentToProduction,
    Preparing,
    Ready,
    Delivered,
    Cancelled
}

public enum ModifierType
{
    Add,
    Remove,
    Extra
}

public enum CrustSelectionMode
{
    None,
    Whole,
    Split
}

public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderItem> _items = [];

    private Order() : base(default) { }

    public Order(
        OrderId id,
        RestaurantUnitId unitId,
        long orderNumber,
        SalesChannel salesChannel,
        FulfillmentType fulfillmentType,
        EmployeeId? createdByEmployeeId = null,
        DeviceId? createdByDeviceId = null,
        TableSessionId? tableSessionId = null) : base(id)
    {
        UnitId = unitId;
        OrderNumber = orderNumber;
        SalesChannel = salesChannel;
        FulfillmentType = fulfillmentType;
        CreatedByEmployeeId = createdByEmployeeId;
        CreatedByDeviceId = createdByDeviceId;
        TableSessionId = tableSessionId;
        Status = OrderStatus.Draft;
        PaymentStatus = OrderPaymentStatus.Pending;
        Subtotal = ServiceFee = DeliveryFee = Discount = Total = Money.Zero();
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public long OrderNumber { get; private set; }
    public TableSessionId? TableSessionId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public SalesChannel SalesChannel { get; private set; }
    public FulfillmentType FulfillmentType { get; private set; }
    public OrderStatus Status { get; private set; }
    public OrderPaymentStatus PaymentStatus { get; private set; }
    public Money Subtotal { get; private set; }
    public Money ServiceFee { get; private set; }
    public Money DeliveryFee { get; private set; }
    public Money Discount { get; private set; }
    public Money Total { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? PlacedAt { get; private set; }
    public EmployeeId? CreatedByEmployeeId { get; private set; }
    public DeviceId? CreatedByDeviceId { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public OrderItem AddItem(
        OrderItemId id,
        ProductId productId,
        string productNameSnapshot,
        int quantity,
        Money unitPrice,
        ProductVariantId? productVariantId = null,
        string? variantNameSnapshot = null,
        string? notes = null)
    {
        EnsureMutable();
        var item = new OrderItem(
            id,
            Id,
            productId,
            productNameSnapshot,
            quantity,
            unitPrice,
            productVariantId,
            variantNameSnapshot,
            notes);
        _items.Add(item);
        RecalculateTotals();
        return item;
    }

    public void RemoveItem(OrderItemId itemId)
    {
        EnsureMutable();
        var item = _items.SingleOrDefault(candidate => candidate.Id == itemId)
            ?? throw new BusinessRuleException("order.item_not_found", "Order item was not found.");
        _items.Remove(item);
        RecalculateTotals();
    }

    public void Submit()
    {
        EnsureStatus(OrderStatus.Draft);
        if (_items.Count == 0)
        {
            throw new BusinessRuleException("order.items_required", "An order must have at least one item.");
        }

        Status = OrderStatus.Submitted;
        PlacedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Accept() => Transition(OrderStatus.Submitted, OrderStatus.Accepted);
    public void StartProduction() => Transition(OrderStatus.Accepted, OrderStatus.InProduction);
    public void MarkReady() => Transition(OrderStatus.InProduction, OrderStatus.Ready);
    public void Complete() => Transition(OrderStatus.Ready, OrderStatus.Completed);

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Cancelled or OrderStatus.Completed)
        {
            throw new BusinessRuleException("order.cannot_cancel", "A completed or cancelled order cannot be cancelled.");
        }

        CancellationReason = Guard.Required(reason, nameof(reason), 500);
        Status = OrderStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void RecalculateTotals(Money? serviceFee = null, Money? deliveryFee = null, Money? discount = null)
    {
        EnsureNotCancelled();
        Subtotal = _items
            .Where(item => item.Status != OrderItemStatus.Cancelled)
            .Aggregate(Money.Zero(), (sum, item) => sum + item.TotalPrice);
        ServiceFee = serviceFee ?? ServiceFee;
        DeliveryFee = deliveryFee ?? DeliveryFee;
        Discount = discount ?? Discount;

        var gross = Subtotal + ServiceFee + DeliveryFee;
        if (Discount.Amount > gross.Amount)
        {
            throw new BusinessRuleException("order.discount", "Discount cannot exceed the gross total.");
        }

        Total = gross - Discount;
        Touch();
    }

    private void EnsureMutable()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new BusinessRuleException("order.not_draft", "Only draft orders can be changed.");
        }
    }

    private void EnsureNotCancelled()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new BusinessRuleException("order.cancelled", "A cancelled order cannot be changed.");
        }
    }

    private void EnsureStatus(OrderStatus status)
    {
        if (Status != status)
        {
            throw new BusinessRuleException("order.invalid_status", $"Expected status {status}, current status is {Status}.");
        }
    }

    private void Transition(OrderStatus expected, OrderStatus next)
    {
        EnsureStatus(expected);
        Status = next;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

public sealed class OrderItem : AggregateRoot<OrderItemId>
{
    private OrderItem() : base(default) { }

    internal OrderItem(
        OrderItemId id,
        OrderId orderId,
        ProductId productId,
        string productNameSnapshot,
        int quantity,
        Money unitPrice,
        ProductVariantId? productVariantId,
        string? variantNameSnapshot,
        string? notes) : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductVariantId = productVariantId;
        ProductNameSnapshot = Guard.Required(productNameSnapshot, nameof(productNameSnapshot), 140);
        VariantNameSnapshot = string.IsNullOrWhiteSpace(variantNameSnapshot) ? null : Guard.Required(variantNameSnapshot, nameof(variantNameSnapshot), 100);
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitPrice = unitPrice;
        TotalPrice = unitPrice * quantity;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : Guard.Required(notes, nameof(notes), 1000);
        Status = OrderItemStatus.Pending;
    }

    public OrderId OrderId { get; private set; }
    public ProductId ProductId { get; private set; }
    public ProductVariantId? ProductVariantId { get; private set; }
    public string ProductNameSnapshot { get; private set; } = string.Empty;
    public string? VariantNameSnapshot { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalPrice { get; private set; }
    public OrderItemStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public ProductionStationId? ProductionStationId { get; private set; }
    public DateTimeOffset? SentToProductionAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
}

public sealed class OrderItemPizza : AggregateRoot<OrderItemId>
{
    private readonly List<OrderItemPizzaFlavor> _flavors = [];

    private OrderItemPizza() : base(default) { }

    public OrderItemPizza(
        OrderItemId orderItemId,
        PizzaSizeId pizzaSizeId,
        string sizeNameSnapshot,
        int sliceCountSnapshot,
        int sizeMaxFlavors,
        PizzaPricingPolicy pricingPolicySnapshot,
        Money basePrice,
        PizzaCrustId? pizzaCrustId = null,
        string? crustNameSnapshot = null,
        PizzaCrustId? secondPizzaCrustId = null,
        string? secondCrustNameSnapshot = null,
        Money? crustPrice = null,
        Money? extrasPrice = null) : base(orderItemId)
    {
        PizzaSizeId = pizzaSizeId;
        SizeNameSnapshot = Guard.Required(sizeNameSnapshot, nameof(sizeNameSnapshot), 80);
        SliceCountSnapshot = Guard.Positive(sliceCountSnapshot, nameof(sliceCountSnapshot));
        if (sizeMaxFlavors is < 1 or > 3)
        {
            throw new BusinessRuleException("pizza.size_limit", "Pizza size flavor limit must be between one and three.");
        }

        SizeMaxFlavors = sizeMaxFlavors;
        PricingPolicySnapshot = pricingPolicySnapshot;
        BasePrice = basePrice;
        if (secondPizzaCrustId.HasValue && !pizzaCrustId.HasValue)
        {
            throw new BusinessRuleException("pizza.crust_first_half", "The first crust half is required.");
        }

        if (secondPizzaCrustId.HasValue && secondPizzaCrustId == pizzaCrustId)
        {
            throw new BusinessRuleException("pizza.crust_duplicate_half", "Split crust halves must be different.");
        }

        PizzaCrustId = pizzaCrustId;
        CrustNameSnapshot = pizzaCrustId.HasValue
            ? Guard.Required(crustNameSnapshot, nameof(crustNameSnapshot), 100)
            : null;
        SecondPizzaCrustId = secondPizzaCrustId;
        SecondCrustNameSnapshot = secondPizzaCrustId.HasValue
            ? Guard.Required(secondCrustNameSnapshot, nameof(secondCrustNameSnapshot), 100)
            : null;
        CrustSelectionMode = secondPizzaCrustId.HasValue
            ? CrustSelectionMode.Split
            : pizzaCrustId.HasValue
                ? CrustSelectionMode.Whole
                : CrustSelectionMode.None;
        CrustPrice = crustPrice ?? Money.Zero();
        ExtrasPrice = extrasPrice ?? Money.Zero();
    }

    public OrderItemId OrderItemId => Id;
    public PizzaSizeId PizzaSizeId { get; private set; }
    public string SizeNameSnapshot { get; private set; } = string.Empty;
    public int SliceCountSnapshot { get; private set; }
    public int SizeMaxFlavors { get; private set; }
    public PizzaCrustId? PizzaCrustId { get; private set; }
    public string? CrustNameSnapshot { get; private set; }
    public PizzaCrustId? SecondPizzaCrustId { get; private set; }
    public string? SecondCrustNameSnapshot { get; private set; }
    public CrustSelectionMode CrustSelectionMode { get; private set; }
    public int FlavorCount { get; private set; }
    public PizzaPricingPolicy PricingPolicySnapshot { get; private set; }
    public Money BasePrice { get; private set; }
    public Money CrustPrice { get; private set; }
    public Money ExtrasPrice { get; private set; }
    public IReadOnlyCollection<OrderItemPizzaFlavor> Flavors => _flavors.AsReadOnly();

    public void AddFlavor(
        OrderItemPizzaFlavorId id,
        PizzaFlavorId pizzaFlavorId,
        string flavorNameSnapshot,
        Money calculatedPrice,
        bool allowRepeatedFlavors = false)
    {
        if (_flavors.Count >= Math.Min(3, SizeMaxFlavors))
        {
            throw new BusinessRuleException("pizza.flavor_limit", "Pizza flavor limit was exceeded.");
        }

        if (!allowRepeatedFlavors && _flavors.Any(flavor => flavor.PizzaFlavorId == pizzaFlavorId))
        {
            throw new BusinessRuleException("pizza.repeated_flavor", "Repeated flavors are not allowed.");
        }

        var countAfterAdd = _flavors.Count + 1;
        _flavors.Add(new OrderItemPizzaFlavor(
            id,
            Id,
            pizzaFlavorId,
            flavorNameSnapshot,
            countAfterAdd,
            countAfterAdd,
            calculatedPrice));
        RebalanceParts();
        FlavorCount = _flavors.Count;
    }

    public void EnsureValidComposition()
    {
        if (_flavors.Count == 0)
        {
            throw new BusinessRuleException("pizza.flavor_required", "Pizza needs at least one flavor.");
        }

        if (FlavorCount != _flavors.Count)
        {
            throw new BusinessRuleException("pizza.flavor_count", "Flavor count does not match the composition.");
        }
    }

    private void RebalanceParts()
    {
        for (var index = 0; index < _flavors.Count; index++)
        {
            _flavors[index].SetPart(index + 1, _flavors.Count);
        }
    }
}

public sealed class OrderItemPizzaFlavor : Entity<OrderItemPizzaFlavorId>
{
    private OrderItemPizzaFlavor() : base(default) { }

    internal OrderItemPizzaFlavor(
        OrderItemPizzaFlavorId id,
        OrderItemId orderItemId,
        PizzaFlavorId pizzaFlavorId,
        string flavorNameSnapshot,
        int partNumber,
        int totalParts,
        Money calculatedPrice) : base(id)
    {
        OrderItemId = orderItemId;
        PizzaFlavorId = pizzaFlavorId;
        FlavorNameSnapshot = Guard.Required(flavorNameSnapshot, nameof(flavorNameSnapshot), 120);
        CalculatedPrice = calculatedPrice;
        SetPart(partNumber, totalParts);
    }

    public OrderItemId OrderItemId { get; private set; }
    public PizzaFlavorId PizzaFlavorId { get; private set; }
    public string FlavorNameSnapshot { get; private set; } = string.Empty;
    public int PartNumber { get; private set; }
    public int TotalParts { get; private set; }
    public Money CalculatedPrice { get; private set; }

    internal void SetPart(int partNumber, int totalParts)
    {
        if (partNumber <= 0 || totalParts <= 0 || partNumber > totalParts || totalParts > 3)
        {
            throw new BusinessRuleException("pizza.invalid_parts", "Pizza flavor parts are invalid.");
        }

        PartNumber = partNumber;
        TotalParts = totalParts;
    }
}

public sealed class OrderItemModifier : Entity<OrderItemModifierId>
{
    private OrderItemModifier() : base(default) { }

    public OrderItemModifier(
        OrderItemModifierId id,
        OrderItemId orderItemId,
        ModifierType modifierType,
        string nameSnapshot,
        decimal quantity,
        Money unitPrice,
        PizzaFlavorId? pizzaFlavorId = null,
        IngredientId? ingredientId = null,
        Guid? optionId = null) : base(id)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("modifier.quantity", "Modifier quantity must be greater than zero.");
        }

        OrderItemId = orderItemId;
        ModifierType = modifierType;
        NameSnapshot = Guard.Required(nameSnapshot, nameof(nameSnapshot), 120);
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalPrice = unitPrice * quantity;
        PizzaFlavorId = pizzaFlavorId;
        IngredientId = ingredientId;
        OptionId = optionId;
    }

    public OrderItemId OrderItemId { get; private set; }
    public PizzaFlavorId? PizzaFlavorId { get; private set; }
    public ModifierType ModifierType { get; private set; }
    public IngredientId? IngredientId { get; private set; }
    public Guid? OptionId { get; private set; }
    public string NameSnapshot { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalPrice { get; private set; }
}
