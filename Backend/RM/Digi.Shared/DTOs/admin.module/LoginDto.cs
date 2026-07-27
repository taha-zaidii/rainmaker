namespace Digi.Shared.DTOs.admin.module
{
    public class LoginDto
    {
        public string? userEmail { get; set; }
        public string? Password { get; set; }

        public string? DeviceToken { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceModel { get; set; }
        public string? OSVersion { get; set; }
        public string? AppVersion { get; set; }
        public string? MacAddress { get; set; }
        public string? DeviceUniqueId { get; set; }
        public bool IsDeviceUniqueId { get; set; }
    }
}
