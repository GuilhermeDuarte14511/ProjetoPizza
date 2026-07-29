using System.Security.Cryptography;
using System.Text;

namespace ProjetoPizza.Application.Devices;

internal static class DeviceProvisioningTokens
{
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
