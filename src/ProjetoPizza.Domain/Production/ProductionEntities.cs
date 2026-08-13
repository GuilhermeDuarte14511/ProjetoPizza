using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Production;

public enum KitchenTicketStatus
{
    New,
    Confirmed,
    Preparing,
    Ready,
    Dispatched,
    Cancelled
}

public sealed class ProductionStation : AggregateRoot<ProductionStationId>
{
    private ProductionStation() : base(default) { }

    public ProductionStation(ProductionStationId id, RestaurantUnitId unitId, string name, string code, int targetPreparationMinutes, int displayOrder = 0) : base(id)
    {
        UnitId = unitId;
        Name = Guard.Required(name, nameof(name), 100);
        Code = Guard.Required(code, nameof(code), 30);
        TargetPreparationMinutes = Guard.Positive(targetPreparationMinutes, nameof(targetPreparationMinutes));
        DisplayOrder = (int)Guard.NonNegative(displayOrder, nameof(displayOrder));
        IsActive = true;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public int TargetPreparationMinutes { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string name,
        string code,
        int targetPreparationMinutes,
        int displayOrder,
        bool isActive)
    {
        Name = Guard.Required(name, nameof(name), 100);
        Code = Guard.Required(code, nameof(code), 30);
        TargetPreparationMinutes = Guard.Positive(targetPreparationMinutes, nameof(targetPreparationMinutes));
        DisplayOrder = (int)Guard.NonNegative(displayOrder, nameof(displayOrder));
        IsActive = isActive;
    }
}

public sealed class KitchenTicket : AggregateRoot<KitchenTicketId>
{
    private KitchenTicket() : base(default) { }

    public KitchenTicket(KitchenTicketId id, RestaurantUnitId unitId, OrderId orderId, ProductionStationId productionStationId, long ticketNumber) : base(id)
    {
        UnitId = unitId;
        OrderId = orderId;
        ProductionStationId = productionStationId;
        TicketNumber = ticketNumber;
        Status = KitchenTicketStatus.New;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public OrderId OrderId { get; private set; }
    public ProductionStationId ProductionStationId { get; private set; }
    public long TicketNumber { get; private set; }
    public KitchenTicketStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }

    public void Confirm()
    {
        Transition(KitchenTicketStatus.New, KitchenTicketStatus.Confirmed);
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    public void StartPreparation()
    {
        Transition(KitchenTicketStatus.Confirmed, KitchenTicketStatus.Preparing);
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void MarkReady()
    {
        Transition(KitchenTicketStatus.Preparing, KitchenTicketStatus.Ready);
        ReadyAt = DateTimeOffset.UtcNow;
    }

    public void Dispatch()
    {
        Transition(KitchenTicketStatus.Ready, KitchenTicketStatus.Dispatched);
        DispatchedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is KitchenTicketStatus.Dispatched or KitchenTicketStatus.Cancelled)
        {
            throw new BusinessRuleException("kitchen_ticket.cannot_cancel", "A dispatched or cancelled kitchen ticket cannot be cancelled.");
        }
        Status = KitchenTicketStatus.Cancelled;
    }

    private void Transition(KitchenTicketStatus expected, KitchenTicketStatus next)
    {
        if (Status != expected)
        {
            throw new BusinessRuleException(
                "kitchen_ticket.invalid_transition",
                $"Kitchen ticket cannot transition from {Status} to {next}.");
        }

        Status = next;
    }
}

public sealed class KitchenTicketItem : Entity<KitchenTicketItemId>
{
    private KitchenTicketItem() : base(default) { }

    public KitchenTicketItem(KitchenTicketItemId id, KitchenTicketId kitchenTicketId, OrderItemId orderItemId, int quantity) : base(id)
    {
        KitchenTicketId = kitchenTicketId;
        OrderItemId = orderItemId;
        Quantity = Guard.Positive(quantity, nameof(quantity));
        Status = KitchenTicketStatus.New;
    }

    public KitchenTicketId KitchenTicketId { get; private set; }
    public OrderItemId OrderItemId { get; private set; }
    public int Quantity { get; private set; }
    public KitchenTicketStatus Status { get; private set; }
}
