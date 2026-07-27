using Digi.Shared.Helper;
using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.notification
{
    /// <summary>
    /// Firebase device token registration DTO
    /// </summary>
    public class FirebaseDeviceTokenDto
    {
        public int DeviceTokenID { get; set; }
        public int CompanyID { get; set; }
        public int UserID { get; set; }
        [Required]
        public string DeviceToken { get; set; } = string.Empty;
        public string DeviceType { get; set; } = "Android"; // Android, iOS, Web
        public string? DeviceName { get; set; }
        public string? DeviceModel { get; set; }
        public string? OSVersion { get; set; }
        public string? AppVersion { get; set; }
        public string? MacAddress { get; set; }
        public string? DeviceUniqueId { get; set; }
        public bool IsDeviceUniqueId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime LastUsedOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? UserName { get; set; }
    }

    /// <summary>
    /// Register device token request DTO
    /// </summary>
    public class RegisterDeviceTokenRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        public string DeviceToken { get; set; } = string.Empty;
        
        public string DeviceType { get; set; } = "Android";
        public string DeviceName { get; set; }
        public string DeviceModel { get; set; }
        public string OSVersion { get; set; }
        public string AppVersion { get; set; }
        
        [Required]
        public string CreatedBy { get; set; }
        /// <summary>Hardware MAC (optional). Saved as-is — not Android build id (e.g. RP1A...).</summary>
        public string? MacAddress { get; set; }

        public bool IsDeviceUniqueId { get; set; }

        /// <summary>Stable per-install id (required for mobile). e.g. ANDROID_ID or app UUID. Saved as-is.</summary>
        public string? DeviceUniqueId { get; set; }

        /// <summary>
        /// When true, all other device-token rows for the same CompanyID+UserID are set inactive in the same transaction
        /// before upserting this token as active (single active push target per user).
        /// </summary>
        public bool EnforceSingleActiveDevice { get; set; }
    }

    /// <summary>
    /// Send Firebase push notification request DTO
    /// </summary>
    public class SendFirebaseNotificationRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Body { get; set; } = string.Empty;
        
        public string ImageUrl { get; set; }
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
        public int Priority { get; set; } = 1; // 1=Normal, 2=High
        public string Sound { get; set; } = "default";
        public bool IsSilent { get; set; } = false;
        public string ClickAction { get; set; } = FirebaseFlutterPushHelper.DefaultClickAction;
        public string ChannelId { get; set; } = FirebaseFlutterPushHelper.DefaultChannelId;
    }

    /// <summary>
    /// Send bulk Firebase push notification request DTO
    /// </summary>
    public class SendBulkFirebaseNotificationRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        public List<int> UserIDs { get; set; } = new List<int>();
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Body { get; set; } = string.Empty;
        
        public string ImageUrl { get; set; }
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
        public int Priority { get; set; } = 1;
        public string Sound { get; set; } = "default";
        public bool IsSilent { get; set; } = false;
        public string ClickAction { get; set; } = FirebaseFlutterPushHelper.DefaultClickAction;
        public string ChannelId { get; set; } = FirebaseFlutterPushHelper.DefaultChannelId;
    }

    /// <summary>
    /// Firebase notification response DTO
    /// </summary>
    public class FirebaseNotificationResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string MessageId { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<FirebaseNotificationErrorDto> Errors { get; set; } = new List<FirebaseNotificationErrorDto>();
    }

    /// <summary>
    /// Firebase notification error DTO
    /// </summary>
    public class FirebaseNotificationErrorDto
    {
        public int UserID { get; set; }
        public string DeviceToken { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Firebase configuration DTO
    /// </summary>
    public class FirebaseConfigDto
    {
        public string ProjectId { get; set; }
        public string CredentialsPath { get; set; }
        public string CredentialsJson { get; set; }
    }
}

