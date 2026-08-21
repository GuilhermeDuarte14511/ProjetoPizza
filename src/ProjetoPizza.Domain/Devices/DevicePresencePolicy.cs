namespace ProjetoPizza.Domain.Devices;

public static class DevicePresencePolicy
{
    public static TimeSpan DefaultHeartbeatTimeout => TimeSpan.FromMinutes(2);

    public static DeviceStatus ResolveStatus(Device device, DateTimeOffset now, TimeSpan heartbeatTimeout)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (heartbeatTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(heartbeatTimeout));
        }

        if (device.DeviceType != DeviceType.CustomerTablet || device.Status != DeviceStatus.Online)
        {
            return device.Status;
        }

        return device.LastSeenAt is { } lastSeenAt && now - lastSeenAt <= heartbeatTimeout
            ? DeviceStatus.Online
            : DeviceStatus.Offline;
    }
}
