using System.Security.Cryptography;
using System.Text;

namespace Digi.Shared.Helper
{
    /// <summary>
    /// Matches <c>sp_NotificationDeviceToken_Register</c> DeviceUniqueId generation.
    /// </summary>
    public static class DeviceUniqueIdHelper
    {
        public static string Compute(
            bool isDeviceUniqueId,
            string? macAddress,
            string? deviceType,
            string? deviceName,
            string? deviceModel,
            string? osVersion,
            string? appVersion)
        {
            if (isDeviceUniqueId && !string.IsNullOrWhiteSpace(macAddress))
                return macAddress.Trim();

            var osPart = osVersion ?? string.Empty;
            if (osPart.Length > 10)
                osPart = osPart[..10];

            var appPart = appVersion ?? string.Empty;
            if (appPart.Length > 5)
                appPart = appPart[..5];

            var payload = string.Concat(
                deviceType ?? string.Empty,
                "|",
                deviceName ?? string.Empty,
                "|",
                deviceModel ?? string.Empty,
                "|",
                osPart,
                "|",
                appPart);

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash);
        }
    }
}
