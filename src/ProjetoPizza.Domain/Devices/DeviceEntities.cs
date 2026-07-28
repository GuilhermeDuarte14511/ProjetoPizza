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
        IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : Guard.Required(ipAddress, nameof(ipAddress), 64);
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
}

public sealed class DeviceSession : AggregateRoot<DeviceSessionId>
{
    private DeviceSession() : base(default) { }

    public DeviceSession(DeviceSessionId id, DeviceId deviceId, TableSessionId tableSessionId, string sessionTokenHash) : base(id)
    {
        DeviceId = deviceId;
        TableSessionId = tableSessionId;
        SessionTokenHash = Guard.Required(sessionTokenHash, nameof(sessionTokenHash), 256);
        StartedAt = DateTimeOffset.UtcNow;
    }

    public DeviceId DeviceId { get; private set; }
    public TableSessionId TableSessionId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public string SessionTokenHash { get; private set; } = string.Empty;
    public string? EndedReason { get; private set; }
}
