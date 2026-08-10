using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Dining;

public enum TableSessionStatus
{
    Open,
    BillRequested,
    PaymentPending,
    Closed,
    Cancelled
}

public enum ServiceCallStatus
{
    Pending,
    Acknowledged,
    InProgress,
    Completed,
    Cancelled
}

public sealed class DiningArea : AggregateRoot<DiningAreaId>
{
    private DiningArea() : base(default) { }

    public DiningArea(DiningAreaId id, RestaurantUnitId unitId, string name, int displayOrder = 0) : base(id)
    {
        UnitId = unitId;
        Name = Guard.Required(name, nameof(name), 100);
        DisplayOrder = (int)Guard.NonNegative(displayOrder, nameof(displayOrder));
        IsActive = true;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, int displayOrder, bool isActive)
    {
        Name = Guard.Required(name, nameof(name), 100);
        DisplayOrder = (int)Guard.NonNegative(displayOrder, nameof(displayOrder));
        IsActive = isActive;
    }
}

public sealed class RestaurantTable : AggregateRoot<RestaurantTableId>
{
    private RestaurantTable() : base(default) { }

    public RestaurantTable(RestaurantTableId id, RestaurantUnitId unitId, DiningAreaId diningAreaId, int number, int capacity, string? name = null) : base(id)
    {
        UnitId = unitId;
        DiningAreaId = diningAreaId;
        Number = Guard.Positive(number, nameof(number));
        Capacity = Guard.Positive(capacity, nameof(capacity));
        Name = string.IsNullOrWhiteSpace(name) ? $"Mesa {number:00}" : Guard.Required(name, nameof(name), 80);
        IsActive = true;
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public DiningAreaId DiningAreaId { get; private set; }
    public int Number { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Activate() => ChangeActive(true);
    public void Deactivate() => ChangeActive(false);

    public void ChangeCapacity(int capacity)
    {
        Capacity = Guard.Positive(capacity, nameof(capacity));
        Touch();
    }

    public void Rename(string name)
    {
        Name = Guard.Required(name, nameof(name), 80);
        Touch();
    }

    public void Update(
        DiningAreaId diningAreaId,
        int number,
        string name,
        int capacity,
        int displayOrder,
        bool isActive)
    {
        DiningAreaId = diningAreaId;
        Number = Guard.Positive(number, nameof(number));
        Name = Guard.Required(name, nameof(name), 80);
        Capacity = Guard.Positive(capacity, nameof(capacity));
        DisplayOrder = (int)Guard.NonNegative(displayOrder, nameof(displayOrder));
        IsActive = isActive;
        Touch();
    }

    public void EnsureCanOpenSession()
    {
        if (!IsActive)
        {
            throw new BusinessRuleException("restaurant_table.inactive", "An inactive table cannot start a session.");
        }
    }

    private void ChangeActive(bool value) { IsActive = value; Touch(); }
    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

public sealed class TableSession : AggregateRoot<TableSessionId>
{
    private readonly List<TableSessionTable> _tables = [];

    private TableSession() : base(default) { }

    private TableSession(
        TableSessionId id,
        RestaurantUnitId unitId,
        long sessionNumber,
        int guestCount,
        EmployeeId? openedByEmployeeId,
        DeviceId? openedByDeviceId,
        Percentage serviceFeePercentageSnapshot) : base(id)
    {
        if (openedByEmployeeId.HasValue == openedByDeviceId.HasValue)
        {
            throw new BusinessRuleException(
                "table_session.opening_actor",
                "A table session must be opened by exactly one employee or device.");
        }

        UnitId = unitId;
        SessionNumber = sessionNumber;
        GuestCount = Guard.Positive(guestCount, nameof(guestCount));
        OpenedByEmployeeId = openedByEmployeeId;
        OpenedByDeviceId = openedByDeviceId;
        ServiceFeePercentageSnapshot = serviceFeePercentageSnapshot;
        Status = TableSessionStatus.Open;
        OpenedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public long SessionNumber { get; private set; }
    public TableSessionStatus Status { get; private set; }
    public int GuestCount { get; private set; }
    public EmployeeId? PrimaryWaiterId { get; private set; }
    public Percentage ServiceFeePercentageSnapshot { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public EmployeeId? OpenedByEmployeeId { get; private set; }
    public DeviceId? OpenedByDeviceId { get; private set; }
    public DateTimeOffset? BillRequestedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public EmployeeId? ClosedByEmployeeId { get; private set; }
    public string? Notes { get; private set; }
    public IReadOnlyCollection<TableSessionTable> Tables => _tables.AsReadOnly();

    public static TableSession Open(
        TableSessionId id,
        RestaurantUnitId unitId,
        long sessionNumber,
        int guestCount,
        EmployeeId openedByEmployeeId,
        Percentage serviceFeePercentageSnapshot,
        IReadOnlyCollection<RestaurantTable> tables)
    {
        if (tables.Count == 0)
        {
            throw new BusinessRuleException("table_session.tables_required", "A table session needs at least one table.");
        }

        foreach (var table in tables)
        {
            table.EnsureCanOpenSession();
        }

        var session = new TableSession(id, unitId, sessionNumber, guestCount, openedByEmployeeId, null, serviceFeePercentageSnapshot);
        var linkedAt = DateTimeOffset.UtcNow;
        session._tables.AddRange(tables.Select((table, index) =>
            new TableSessionTable(id, table.Id, index == 0, linkedAt, openedByEmployeeId, null)));
        return session;
    }

    public static TableSession OpenFromDevice(
        TableSessionId id,
        RestaurantUnitId unitId,
        long sessionNumber,
        int guestCount,
        DeviceId openedByDeviceId,
        Percentage serviceFeePercentageSnapshot,
        IReadOnlyCollection<RestaurantTable> tables)
    {
        if (tables.Count == 0)
        {
            throw new BusinessRuleException("table_session.tables_required", "A table session needs at least one table.");
        }

        foreach (var table in tables)
        {
            table.EnsureCanOpenSession();
        }

        var session = new TableSession(id, unitId, sessionNumber, guestCount, null, openedByDeviceId, serviceFeePercentageSnapshot);
        var linkedAt = DateTimeOffset.UtcNow;
        session._tables.AddRange(tables.Select((table, index) =>
            new TableSessionTable(id, table.Id, index == 0, linkedAt, null, openedByDeviceId)));
        return session;
    }

    public void AssignWaiter(EmployeeId employeeId) => PrimaryWaiterId = employeeId;
    public void ChangeGuestCount(int guestCount) => GuestCount = Guard.Positive(guestCount, nameof(guestCount));

    public void RequestBill()
    {
        EnsureStatus(TableSessionStatus.Open);
        Status = TableSessionStatus.BillRequested;
        BillRequestedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPaymentPending()
    {
        if (Status is not (TableSessionStatus.Open or TableSessionStatus.BillRequested))
        {
            throw new BusinessRuleException("table_session.payment_status", "This session cannot start payment.");
        }

        Status = TableSessionStatus.PaymentPending;
    }

    public void Close(EmployeeId employeeId)
    {
        if (Status is TableSessionStatus.Closed or TableSessionStatus.Cancelled)
        {
            throw new BusinessRuleException("table_session.already_finished", "The session is already finished.");
        }

        Status = TableSessionStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        ClosedByEmployeeId = employeeId;
    }

    public void Cancel(EmployeeId employeeId, string notes)
    {
        EnsureStatus(TableSessionStatus.Open);
        Status = TableSessionStatus.Cancelled;
        ClosedAt = DateTimeOffset.UtcNow;
        ClosedByEmployeeId = employeeId;
        Notes = Guard.Required(notes, nameof(notes), 500);
    }

    public void EnsureCanReceiveOrders()
    {
        if (Status is TableSessionStatus.Closed or TableSessionStatus.Cancelled)
        {
            throw new BusinessRuleException("table_session.finished", "A finished session cannot receive orders.");
        }
    }

    private void EnsureStatus(TableSessionStatus expected)
    {
        if (Status != expected)
        {
            throw new BusinessRuleException("table_session.invalid_status", $"Expected status {expected}, current status is {Status}.");
        }
    }
}

public sealed class TableSessionTable
{
    private TableSessionTable() { }

    internal TableSessionTable(
        TableSessionId tableSessionId,
        RestaurantTableId restaurantTableId,
        bool isPrimary,
        DateTimeOffset linkedAt,
        EmployeeId? linkedByEmployeeId,
        DeviceId? linkedByDeviceId)
    {
        if (linkedByEmployeeId.HasValue == linkedByDeviceId.HasValue)
        {
            throw new BusinessRuleException(
                "table_session_table.linking_actor",
                "A table must be linked by exactly one employee or device.");
        }

        TableSessionId = tableSessionId;
        RestaurantTableId = restaurantTableId;
        IsPrimary = isPrimary;
        LinkedAt = linkedAt;
        LinkedByEmployeeId = linkedByEmployeeId;
        LinkedByDeviceId = linkedByDeviceId;
    }

    public TableSessionId TableSessionId { get; private set; }
    public RestaurantTableId RestaurantTableId { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset LinkedAt { get; private set; }
    public DateTimeOffset? UnlinkedAt { get; private set; }
    public EmployeeId? LinkedByEmployeeId { get; private set; }
    public DeviceId? LinkedByDeviceId { get; private set; }
}

public sealed class WaiterAssignment : Entity<WaiterAssignmentId>
{
    private WaiterAssignment() : base(default) { }

    public WaiterAssignment(WaiterAssignmentId id, TableSessionId tableSessionId, EmployeeId employeeId, EmployeeId assignedByEmployeeId) : base(id)
    {
        TableSessionId = tableSessionId;
        EmployeeId = employeeId;
        AssignedByEmployeeId = assignedByEmployeeId;
        AssignedAt = DateTimeOffset.UtcNow;
    }

    public TableSessionId TableSessionId { get; private set; }
    public EmployeeId EmployeeId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? UnassignedAt { get; private set; }
    public EmployeeId AssignedByEmployeeId { get; private set; }
}

public sealed class ServiceCallType : AggregateRoot<ServiceCallTypeId>
{
    private ServiceCallType() : base(default) { }

    public ServiceCallType(ServiceCallTypeId id, string code, string name) : base(id)
    {
        Code = Guard.Required(code, nameof(code), 50);
        Name = Guard.Required(name, nameof(name), 100);
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void Update(string code, string name, bool isActive)
    {
        Code = Guard.Required(code, nameof(code), 50);
        Name = Guard.Required(name, nameof(name), 100);
        IsActive = isActive;
    }
}

public sealed class ServiceCall : AggregateRoot<ServiceCallId>
{
    private ServiceCall() : base(default) { }

    public ServiceCall(
        ServiceCallId id,
        RestaurantUnitId unitId,
        TableSessionId tableSessionId,
        ServiceCallTypeId serviceCallTypeId,
        DeviceId requestedByDeviceId,
        string? details = null) : base(id)
    {
        UnitId = unitId;
        TableSessionId = tableSessionId;
        ServiceCallTypeId = serviceCallTypeId;
        RequestedByDeviceId = requestedByDeviceId;
        Details = details;
        Status = ServiceCallStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public TableSessionId TableSessionId { get; private set; }
    public ServiceCallTypeId ServiceCallTypeId { get; private set; }
    public DeviceId RequestedByDeviceId { get; private set; }
    public string? Details { get; private set; }
    public ServiceCallStatus Status { get; private set; }
    public EmployeeId? AssignedEmployeeId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public void Acknowledge(EmployeeId employeeId)
    {
        if (Status != ServiceCallStatus.Pending)
        {
            throw new BusinessRuleException("service_call.not_pending", "Only a pending service call can be acknowledged.");
        }

        AssignedEmployeeId = employeeId;
        Status = ServiceCallStatus.Acknowledged;
        AcknowledgedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(EmployeeId employeeId)
    {
        if (Status is ServiceCallStatus.Completed or ServiceCallStatus.Cancelled)
        {
            throw new BusinessRuleException("service_call.closed", "Service call is already closed.");
        }

        AssignedEmployeeId ??= employeeId;
        Status = ServiceCallStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
