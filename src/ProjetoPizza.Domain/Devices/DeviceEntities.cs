using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Devices;

public enum DeviceType
{
    CustomerTablet,
    KitchenDisplay,
    PointOfSale,
    Printer,
    Administrative
}

public enum DeviceStatus
{
    Online,
    Offline,
    Idle,
    Blocked,
    Maintenance
}

public enum PrintDocumentType
{
    TestPage,
    KitchenTicket,
    CustomerReceipt,
    CashClosing,
    FiscalDocument
}

public enum PrintJobStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public sealed class Device : AggregateRoot<DeviceId>
{
    private Device() : base(default) { }

    public Device(DeviceId id, RestaurantUnitId unitId, string name, string serialNumber, DeviceType deviceType, string platform) : base(id)
    {
        UnitId = unitId;
        Name = Guard.Required(name, nameof(name), 100);
        SerialNumber = Guard.Required(serialNumber, nameof(serialNumber), 100);
        DeviceType = deviceType;
        Platform = Guard.Required(platform, nameof(platform), 60);
        Status = DeviceStatus.Offline;
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public DeviceType DeviceType { get; private set; }
    public string Platform { get; private set; } = string.Empty;
    public string? AppVersion { get; private set; }
    public DeviceStatus Status { get; private set; }
    public int? BatteryPercentage { get; private set; }
    public bool IsCharging { get; private set; }
    public string? NetworkStatus { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public RestaurantTableId? LinkedTableId { get; private set; }
    public bool IsLocked { get; private set; }
    public int? PrinterPort { get; private set; }
    public int? PaperWidthMm { get; private set; }
    public bool AutoPrintKitchenTickets { get; private set; }
    public bool AutoPrintCustomerReceipts { get; private set; }
    public bool AutoPrintFiscalDocuments { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateStatus(
        DeviceStatus status,
        int? batteryPercentage,
        bool isCharging,
        string? networkStatus,
        string? ipAddress,
        string? appVersion)
    {
        if (batteryPercentage is < 0 or > 100)
        {
            throw new BusinessRuleException("device.battery", "Battery percentage must be between zero and one hundred.");
        }

        Status = status;
        BatteryPercentage = batteryPercentage;
        IsCharging = isCharging;
        NetworkStatus = string.IsNullOrWhiteSpace(networkStatus) ? null : Guard.Required(networkStatus, nameof(networkStatus), 60);
        IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : Guard.Required(ipAddress, nameof(ipAddress), 255);
        AppVersion = string.IsNullOrWhiteSpace(appVersion) ? null : Guard.Required(appVersion, nameof(appVersion), 40);
        LastSeenAt = DateTimeOffset.UtcNow;
        UpdatedAt = LastSeenAt.Value;
    }

    public void LinkToTable(RestaurantTableId? tableId)
    {
        LinkedTableId = tableId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetLocked(bool value)
    {
        IsLocked = value;
        Status = value ? DeviceStatus.Blocked : DeviceStatus.Offline;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ConfigureNetworkPrinter(
        string name,
        string host,
        int port,
        int paperWidthMm,
        bool autoPrintKitchenTickets,
        bool autoPrintCustomerReceipts,
        bool autoPrintFiscalDocuments)
    {
        if (DeviceType != DeviceType.Printer)
            throw new BusinessRuleException("device.printer_type", "Only printer devices accept printer configuration.");
        if (port is < 1 or > 65535)
            throw new BusinessRuleException("device.printer_port", "Printer port must be between 1 and 65535.");
        if (paperWidthMm is not (58 or 80))
            throw new BusinessRuleException("device.printer_paper", "Printer paper width must be 58 or 80 mm.");

        Name = Guard.Required(name, nameof(name), 100);
        IpAddress = Guard.Required(host, nameof(host), 255);
        PrinterPort = port;
        PaperWidthMm = paperWidthMm;
        AutoPrintKitchenTickets = autoPrintKitchenTickets;
        AutoPrintCustomerReceipts = autoPrintCustomerReceipts;
        AutoPrintFiscalDocuments = autoPrintFiscalDocuments;
        Platform = "ESC/POS TCP";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class PrintJob : AggregateRoot<PrintJobId>
{
    private PrintJob() : base(default) { }

    public PrintJob(
        PrintJobId id,
        RestaurantUnitId unitId,
        DeviceId printerId,
        PrintDocumentType documentType,
        string payload,
        int copies = 1) : base(id)
    {
        if (copies is < 1 or > 5)
            throw new BusinessRuleException("print_job.copies", "Print copies must be between one and five.");
        UnitId = unitId;
        PrinterId = printerId;
        DocumentType = documentType;
        Payload = Guard.Required(payload, nameof(payload), 20000);
        Copies = copies;
        Status = PrintJobStatus.Pending;
        NextAttemptAt = CreatedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public DeviceId PrinterId { get; private set; }
    public PrintDocumentType DocumentType { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public int Copies { get; private set; }
    public PrintJobStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public void Start()
    {
        if (Status is not (PrintJobStatus.Pending or PrintJobStatus.Failed))
            throw new BusinessRuleException("print_job.status", "Only pending or failed print jobs can start.");
        Status = PrintJobStatus.Processing;
        Attempts++;
    }

    public void Complete()
    {
        if (Status != PrintJobStatus.Processing)
            throw new BusinessRuleException("print_job.status", "Only processing print jobs can complete.");
        Status = PrintJobStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        LastError = null;
    }

    public void Fail(string error)
    {
        if (Status != PrintJobStatus.Processing)
            throw new BusinessRuleException("print_job.status", "Only processing print jobs can fail.");
        LastError = Guard.Required(error, nameof(error), 1000);
        Status = PrintJobStatus.Failed;
        NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, Attempts) * 5));
    }
}

public sealed class DeviceSession : AggregateRoot<DeviceSessionId>
{
    private DeviceSession() : base(default) { }

    public DeviceSession(
        DeviceSessionId id,
        DeviceId deviceId,
        string sessionTokenHash,
        TableSessionId? tableSessionId = null,
        DateTimeOffset? expiresAt = null) : base(id)
    {
        if (expiresAt.HasValue && expiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new BusinessRuleException(
                "device_session.expiration",
                "A device session expiration must be in the future.");
        }

        DeviceId = deviceId;
        TableSessionId = tableSessionId;
        SessionTokenHash = Guard.Required(sessionTokenHash, nameof(sessionTokenHash), 256);
        StartedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    public DeviceId DeviceId { get; private set; }
    public TableSessionId? TableSessionId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public string SessionTokenHash { get; private set; } = string.Empty;
    public string? EndedReason { get; private set; }

    public bool IsAvailableAt(DateTimeOffset now) =>
        EndedAt is null && (!ExpiresAt.HasValue || ExpiresAt.Value > now);

    public void BindToTableSession(TableSessionId tableSessionId)
    {
        if (EndedAt.HasValue)
        {
            throw new BusinessRuleException(
                "device_session.ended",
                "An ended device session cannot be linked to a table session.");
        }

        TableSessionId = tableSessionId;
    }

    public void ClearTableSession() => TableSessionId = null;

    public void End(string reason)
    {
        if (EndedAt.HasValue)
        {
            return;
        }

        EndedReason = Guard.Required(reason, nameof(reason), 200);
        EndedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class DeviceProvisioning : AggregateRoot<DeviceProvisioningId>
{
    private DeviceProvisioning() : base(default) { }

    public DeviceProvisioning(
        DeviceProvisioningId id,
        DeviceId deviceId,
        string tokenHash,
        DateTimeOffset expiresAt) : base(id)
    {
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new BusinessRuleException(
                "device_provisioning.expiration",
                "A device provisioning expiration must be in the future.");
        }

        DeviceId = deviceId;
        TokenHash = Guard.Required(tokenHash, nameof(tokenHash), 64);
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    public DeviceId DeviceId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsAvailableAt(DateTimeOffset now) =>
        ConsumedAt is null && RevokedAt is null && ExpiresAt > now;

    public void Consume()
    {
        if (!IsAvailableAt(DateTimeOffset.UtcNow))
        {
            throw new BusinessRuleException(
                "device_provisioning.unavailable",
                "The device provisioning credential is no longer available.");
        }

        ConsumedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke()
    {
        if (ConsumedAt is null && RevokedAt is null)
        {
            RevokedAt = DateTimeOffset.UtcNow;
        }
    }
}
