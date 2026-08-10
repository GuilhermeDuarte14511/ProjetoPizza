using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Billing;

public enum BillStatus
{
    Open,
    Requested,
    PaymentInProgress,
    Paid,
    Cancelled
}

public enum BillSplitStatus
{
    Open,
    PartiallyPaid,
    Paid,
    Cancelled
}

public enum PaymentStatus
{
    Pending,
    Authorized,
    Paid,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded
}

public sealed class Bill : AggregateRoot<BillId>
{
    private Bill() : base(default) { }

    public Bill(BillId id, RestaurantUnitId unitId, TableSessionId tableSessionId, Money subtotal, Percentage serviceFeePercentage) : base(id)
    {
        UnitId = unitId;
        TableSessionId = tableSessionId;
        InitializeAmounts(subtotal, serviceFeePercentage, Money.Zero());
    }

    public Bill(BillId id, RestaurantUnitId unitId, OrderId orderId, Money subtotal, Money discountAmount) : base(id)
    {
        UnitId = unitId;
        OrderId = orderId;
        InitializeAmounts(subtotal, new Percentage(0), discountAmount);
    }

    public RestaurantUnitId UnitId { get; private set; }
    public TableSessionId? TableSessionId { get; private set; }
    public OrderId? OrderId { get; private set; }
    public BillStatus Status { get; private set; }
    public Money Subtotal { get; private set; }
    public Percentage ServiceFeePercentage { get; private set; }
    public Money ServiceFeeAmount { get; private set; }
    public Money DiscountAmount { get; private set; }
    public Money TotalAmount { get; private set; }
    public Money PaidAmount { get; private set; }
    public Money RemainingAmount { get; private set; }
    public DateTimeOffset? RequestedAt { get; private set; }
    public int? RequestedSplitCount { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private void InitializeAmounts(Money subtotal, Percentage serviceFeePercentage, Money discountAmount)
    {
        if (discountAmount.Amount > subtotal.Amount + (subtotal * serviceFeePercentage.AsFactor).Amount)
        {
            throw new BusinessRuleException("bill.discount", "Discount cannot exceed the bill amount.");
        }

        Subtotal = subtotal;
        ServiceFeePercentage = serviceFeePercentage;
        ServiceFeeAmount = subtotal * serviceFeePercentage.AsFactor;
        DiscountAmount = discountAmount;
        PaidAmount = Money.Zero();
        TotalAmount = subtotal + ServiceFeeAmount - discountAmount;
        RemainingAmount = TotalAmount;
        Status = BillStatus.Open;
    }

    public void Request(int? splitCount = null)
    {
        if (splitCount is not null and (< 2 or > 50))
        {
            throw new BusinessRuleException(
                "bill.split_count",
                "A split bill must contain between 2 and 50 people.");
        }

        if (Status is not (BillStatus.Open or BillStatus.Requested))
        {
            throw new BusinessRuleException("bill.not_requestable", "Only an open or requested bill can be requested.");
        }

        Status = BillStatus.Requested;
        RequestedAt ??= DateTimeOffset.UtcNow;
        RequestedSplitCount = splitCount;
    }

    public void RegisterPayment(Money amount)
    {
        if (Status is BillStatus.Paid or BillStatus.Cancelled)
        {
            throw new BusinessRuleException("bill.closed", "A closed bill cannot receive payments.");
        }

        if (amount.Amount <= 0 || amount.Amount > RemainingAmount.Amount)
        {
            throw new BusinessRuleException("bill.invalid_payment", "Payment must be positive and not exceed the remaining amount.");
        }

        PaidAmount += amount;
        RemainingAmount -= amount;
        ConfirmedAt ??= DateTimeOffset.UtcNow;
        Status = RemainingAmount.Amount == 0 ? BillStatus.Paid : BillStatus.PaymentInProgress;
        if (Status == BillStatus.Paid)
        {
            ClosedAt = DateTimeOffset.UtcNow;
        }
    }
}

public sealed class BillItem : Entity<BillItemId>
{
    private BillItem() : base(default) { }

    public BillItem(BillItemId id, BillId billId, OrderItemId orderItemId, decimal quantity, Money grossAmount, Money serviceFeeAmount, Money discountAmount) : base(id)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("bill_item.quantity", "Bill item quantity must be greater than zero.");
        }

        BillId = billId;
        OrderItemId = orderItemId;
        Quantity = quantity;
        GrossAmount = grossAmount;
        ServiceFeeAmount = serviceFeeAmount;
        DiscountAmount = discountAmount;
        var grossWithFee = grossAmount + serviceFeeAmount;
        NetAmount = grossWithFee - discountAmount;
    }

    public BillId BillId { get; private set; }
    public OrderItemId OrderItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public Money GrossAmount { get; private set; }
    public Money ServiceFeeAmount { get; private set; }
    public Money DiscountAmount { get; private set; }
    public Money NetAmount { get; private set; }
}

public sealed class BillSplit : AggregateRoot<BillSplitId>
{
    private BillSplit() : base(default) { }

    public BillSplit(BillSplitId id, BillId billId, string name, int splitNumber, Money totalAmount) : base(id)
    {
        BillId = billId;
        Name = Guard.Required(name, nameof(name), 100);
        SplitNumber = Guard.Positive(splitNumber, nameof(splitNumber));
        TotalAmount = RemainingAmount = totalAmount;
        PaidAmount = Money.Zero();
        Status = BillSplitStatus.Open;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public BillId BillId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int SplitNumber { get; private set; }
    public BillSplitStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    public Money PaidAmount { get; private set; }
    public Money RemainingAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void RegisterPayment(Money amount)
    {
        if (Status is BillSplitStatus.Paid or BillSplitStatus.Cancelled)
        {
            throw new BusinessRuleException("bill_split.closed", "A closed bill split cannot receive payments.");
        }

        if (amount.Amount <= 0 || amount.Amount > RemainingAmount.Amount)
        {
            throw new BusinessRuleException("bill_split.invalid_payment", "Payment must be positive and not exceed the split remaining amount.");
        }

        PaidAmount += amount;
        RemainingAmount -= amount;
        Status = RemainingAmount.Amount == 0 ? BillSplitStatus.Paid : BillSplitStatus.PartiallyPaid;
    }
}

public sealed class BillSplitItem
{
    private BillSplitItem() { }

    public BillSplitItem(BillSplitId billSplitId, BillItemId billItemId, decimal quantity, Money allocatedAmount)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("bill_split_item.quantity", "Allocated quantity must be greater than zero.");
        }

        BillSplitId = billSplitId;
        BillItemId = billItemId;
        Quantity = quantity;
        AllocatedAmount = allocatedAmount;
    }

    public BillSplitId BillSplitId { get; private set; }
    public BillItemId BillItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public Money AllocatedAmount { get; private set; }
}

public sealed class PaymentMethod : AggregateRoot<PaymentMethodId>
{
    private PaymentMethod() : base(default) { }

    public PaymentMethod(
        PaymentMethodId id,
        RestaurantUnitId unitId,
        string code,
        string name,
        bool requiresExternalReference,
        bool allowsChange,
        int displayOrder = 0) : base(id)
    {
        UnitId = unitId;
        Code = Guard.Required(code, nameof(code), 40);
        Name = Guard.Required(name, nameof(name), 100);
        RequiresExternalReference = requiresExternalReference;
        AllowsChange = allowsChange;
        DisplayOrder = (int)Guard.NonNegative(displayOrder, nameof(displayOrder));
        IsActive = true;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool RequiresExternalReference { get; private set; }
    public bool AllowsChange { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }

    public void Update(
        string code,
        string name,
        bool requiresExternalReference,
        bool allowsChange,
        int displayOrder,
        bool isActive)
    {
        Code = Guard.Required(code, nameof(code), 40);
        Name = Guard.Required(name, nameof(name), 100);
        RequiresExternalReference = requiresExternalReference;
        AllowsChange = allowsChange;
        DisplayOrder = (int)Guard.NonNegative(displayOrder, nameof(displayOrder));
        IsActive = isActive;
    }
}

public sealed class Payment : AggregateRoot<PaymentId>
{
    private Payment() : base(default) { }

    public Payment(
        PaymentId id,
        RestaurantUnitId unitId,
        BillId billId,
        PaymentMethod paymentMethod,
        Money amount,
        Money receivedAmount,
        EmployeeId receivedByEmployeeId,
        BillSplitId? billSplitId = null,
        CashShiftId? cashShiftId = null,
        string? externalReference = null) : base(id)
    {
        if (amount.Amount <= 0)
        {
            throw new BusinessRuleException("payment.amount", "Payment amount must be greater than zero.");
        }

        if (receivedAmount.Amount < amount.Amount)
        {
            throw new BusinessRuleException("payment.received_amount", "Received amount cannot be lower than payment amount.");
        }

        var change = receivedAmount - amount;
        if (change.Amount > 0 && !paymentMethod.AllowsChange)
        {
            throw new BusinessRuleException("payment.change_not_allowed", "This payment method does not allow change.");
        }

        if (paymentMethod.RequiresExternalReference && string.IsNullOrWhiteSpace(externalReference))
        {
            throw new BusinessRuleException("payment.external_reference", "This payment method requires an external reference.");
        }

        UnitId = unitId;
        BillId = billId;
        BillSplitId = billSplitId;
        CashShiftId = cashShiftId;
        PaymentMethodId = paymentMethod.Id;
        Amount = amount;
        ReceivedAmount = receivedAmount;
        ChangeAmount = change;
        ExternalReference = externalReference;
        ReceivedByEmployeeId = receivedByEmployeeId;
        Status = PaymentStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public BillId BillId { get; private set; }
    public BillSplitId? BillSplitId { get; private set; }
    public CashShiftId? CashShiftId { get; private set; }
    public PaymentMethodId PaymentMethodId { get; private set; }
    public PaymentStatus Status { get; private set; }
    public Money Amount { get; private set; }
    public Money ReceivedAmount { get; private set; }
    public Money ChangeAmount { get; private set; }
    public string? ExternalReference { get; private set; }
    public string? AuthorizationCode { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public EmployeeId ReceivedByEmployeeId { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    public void Cancel(string reason)
    {
        if (Status == PaymentStatus.Cancelled)
        {
            throw new BusinessRuleException("payment.already_cancelled", "Payment is already cancelled.");
        }

        if (Status is PaymentStatus.Refunded or PaymentStatus.PartiallyRefunded)
        {
            throw new BusinessRuleException("payment.refunded", "A refunded payment cannot be cancelled.");
        }

        CancellationReason = Guard.Required(reason, nameof(reason), 500);
        Status = PaymentStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
    }
}
