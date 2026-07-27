namespace Digi.Shared.DTOs.notification
{
    /// <summary>
    /// Result row from <c>sp_NotificationDeviceToken_Register</c> (success and error shapes).
    /// </summary>
    public class NotificationDeviceTokenRegisterResultDto
    {
        public bool IsSuccess { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ActiveDeviceName { get; set; }
        public string? ActiveDeviceToken { get; set; }

        public int DeviceTokenID { get; set; }
        public int CompanyID { get; set; }
        public int UserID { get; set; }
        public string? DeviceUniqueId { get; set; }
        public string? DeviceToken { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceModel { get; set; }
        public string? MacAddress { get; set; }
        public bool IsDeviceUniqueId { get; set; }
        public string? OSVersion { get; set; }
        public string? AppVersion { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? LastUsedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public string? UserName { get; set; }
    }
}
