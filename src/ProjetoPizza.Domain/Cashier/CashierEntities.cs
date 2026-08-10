using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Cashier;

public enum CashShiftStatus
{
    Open,
    Closing,
    Closed,
    Cancelled
}

public enum CashMovementType
{
    Opening,
    Sale,
    Supply,
    Withdrawal,
    Refund,
    Adjustment,
    Closing
}

public sealed class CashRegister : AggregateRoot<CashRegisterId>
{
    private CashRegister() : base(default) { }

    public CashRegister(CashRegisterId id, RestaurantUnitId unitId, string name, string code) : base(id)
    {
        UnitId = unitId;
        Name = Guard.Required(name, nameof(name), 100);
        Code = Guard.Required(code, nameof(code), 30);
        IsActive = true;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void Update(string name, string code, bool isActive)
    {
        Name = Guard.Required(name, nameof(name), 100);
        Code = Guard.Required(code, nameof(code), 30);
        IsActive = isActive;
    }
}

public sealed class CashShift : AggregateRoot<CashShiftId>
{
    private readonly List<CashMovement> _movements = [];

    private CashShift() : base(default) { }

    public CashShift(CashShiftId id, CashRegisterId cashRegisterId, EmployeeId operatorEmployeeId, Money openingAmount) : base(id)
    {
        CashRegisterId = cashRegisterId;
        OperatorEmployeeId = operatorEmployeeId;
        OpeningAmount = openingAmount;
        Status = CashShiftStatus.Open;
        OpenedAt = DateTimeOffset.UtcNow;
        ExpectedCashAmount = openingAmount;
    }

    public CashRegisterId CashRegisterId { get; private set; }
    public EmployeeId OperatorEmployeeId { get; private set; }
    public CashShiftStatus Status { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public Money OpeningAmount { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public EmployeeId? ClosedByEmployeeId { get; private set; }
    public Money ExpectedCashAmount { get; private set; }
    public Money? CountedCashAmount { get; private set; }
    public decimal? DifferenceAmount { get; private set; }
    public string? ClosingNotes { get; private set; }
    public IReadOnlyCollection<CashMovement> Movements => _movements.AsReadOnly();

    public CashMovement RegisterMovement(
        CashMovementId id,
        CashMovementType movementType,
        Money amount,
        string description,
        string reason,
        EmployeeId createdByEmployeeId,
        EmployeeId? authorizedByEmployeeId = null,
        PaymentId? paymentId = null)
    {
        if (Status != CashShiftStatus.Open)
        {
            throw new BusinessRuleException("cash_shift.not_open", "Cash movements require an open shift.");
        }

        var movement = new CashMovement(id, Id, movementType, amount, description, reason, createdByEmployeeId, authorizedByEmployeeId, paymentId);
        _movements.Add(movement);
        ExpectedCashAmount = CalculateExpectedAmount();
        return movement;
    }

    public void Close(EmployeeId closedByEmployeeId, Money countedCashAmount, string? notes = null)
    {
        if (Status == CashShiftStatus.Closed)
        {
            throw new BusinessRuleException("cash_shift.already_closed", "Cash shift is already closed.");
        }

        if (Status != CashShiftStatus.Open)
        {
            throw new BusinessRuleException("cash_shift.not_open", "Only an open cash shift can be closed.");
        }

        ExpectedCashAmount = CalculateExpectedAmount();
        CountedCashAmount = countedCashAmount;
        DifferenceAmount = countedCashAmount.Amount - ExpectedCashAmount.Amount;
        ClosingNotes = string.IsNullOrWhiteSpace(notes) ? null : Guard.Required(notes, nameof(notes), 500);
        ClosedByEmployeeId = closedByEmployeeId;
        ClosedAt = DateTimeOffset.UtcNow;
        Status = CashShiftStatus.Closed;
    }

    private Money CalculateExpectedAmount()
    {
        var amount = OpeningAmount.Amount;
        foreach (var movement in _movements)
        {
            amount += movement.MovementType switch
            {
                CashMovementType.Sale or CashMovementType.Supply or CashMovementType.Adjustment => movement.Amount.Amount,
                CashMovementType.Withdrawal or CashMovementType.Refund => -movement.Amount.Amount,
                _ => 0m
            };
        }

        if (amount < 0)
        {
            throw new BusinessRuleException("cash_shift.negative_expected", "Expected cash amount cannot be negative.");
        }

        return new Money(amount);
    }
}

public sealed class CashMovement : Entity<CashMovementId>
{
    private CashMovement() : base(default) { }

    internal CashMovement(
        CashMovementId id,
        CashShiftId cashShiftId,
        CashMovementType movementType,
        Money amount,
        string description,
        string reason,
        EmployeeId createdByEmployeeId,
        EmployeeId? authorizedByEmployeeId,
        PaymentId? paymentId) : base(id)
    {
        CashShiftId = cashShiftId;
        MovementType = movementType;
        PaymentId = paymentId;
        Amount = amount;
        Description = Guard.Required(description, nameof(description), 200);
        Reason = Guard.Required(reason, nameof(reason), 300);
        CreatedByEmployeeId = createdByEmployeeId;
        AuthorizedByEmployeeId = authorizedByEmployeeId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public CashShiftId CashShiftId { get; private set; }
    public CashMovementType MovementType { get; private set; }
    public PaymentId? PaymentId { get; private set; }
    public Money Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public EmployeeId CreatedByEmployeeId { get; private set; }
    public EmployeeId? AuthorizedByEmployeeId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
