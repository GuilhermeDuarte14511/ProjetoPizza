using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Devices;

public sealed class DevicePresencePolicyTests
{
    [Fact]
    public void Customer_tablet_without_recent_heartbeat_is_offline()
    {
        var device = new Device(
            DeviceId.New(),
            RestaurantUnitId.New(),
            "Tablet salão 32",
            "TAB-TESTE",
            DeviceType.CustomerTablet,
            "Android");

        device.UpdateStatus(DeviceStatus.Online, 72, false, "Online", "192.168.15.32", "1.0.0");

        var status = DevicePresencePolicy.ResolveStatus(
            device,
            device.LastSeenAt!.Value.AddMinutes(2),
            TimeSpan.FromMinutes(1));

        Assert.Equal(DeviceStatus.Offline, status);
    }

    [Fact]
    public void Customer_tablet_with_recent_heartbeat_remains_online()
    {
        var device = new Device(
            DeviceId.New(),
            RestaurantUnitId.New(),
            "Tablet salão 32",
            "TAB-TESTE",
            DeviceType.CustomerTablet,
            "Android");

        device.UpdateStatus(DeviceStatus.Online, 72, false, "Online", "192.168.15.32", "1.0.0");

        var status = DevicePresencePolicy.ResolveStatus(
            device,
            device.LastSeenAt!.Value.AddSeconds(30),
            TimeSpan.FromMinutes(1));

        Assert.Equal(DeviceStatus.Online, status);
    }
}
