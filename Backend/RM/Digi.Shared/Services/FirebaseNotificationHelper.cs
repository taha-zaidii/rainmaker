using Digi.Shared.DTOs.notification;
using Digi.Shared.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Digi.Shared.Services
{
    public interface IFirebaseNotificationHelper
    {
        Task SendNotificationToFirebaseAsync(int notificationID, int? companyID, int? toUserID, string title, string message, string link = null);
        Task<bool> SendPushToUserAsync(int companyId, int userId, string title, string message, string? link = null, string? sourceAction = null, int? notificationId = null);
    }

    public class FirebaseNotificationHelper : IFirebaseNotificationHelper
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FirebaseNotificationHelper> _logger;
        private readonly string _apiGatewayBaseUrl;

        public FirebaseNotificationHelper(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<FirebaseNotificationHelper> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _apiGatewayBaseUrl = configuration["AppSettings:BaseUrl"] ?? "https://localhost:7777";
        }

        public async Task SendNotificationToFirebaseAsync(int notificationID, int? companyID, int? toUserID, string title, string message, string link = null)
        {
            if (!companyID.HasValue || !toUserID.HasValue)
            {
                _logger.LogWarning("Cannot send notification {NotificationID}: CompanyID or ToUserID is null", notificationID);
                return;
            }
            await SendPushToUserAsync(companyID.Value, toUserID.Value, title, message, link, "hr_notification", notificationID);
        }

        public async Task<bool> SendPushToUserAsync(int companyId, int userId, string title, string message, string? link = null, string? sourceAction = null, int? notificationId = null)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                var request = new SendFirebaseNotificationRequestDto
                {
                    CompanyID = companyId,
                    UserID = userId,
                    Title = title ?? "Notification",
                    Body = message ?? "",
                    Priority = 2,
                    Sound = "default",
                    ChannelId = FirebaseFlutterPushHelper.DefaultChannelId,
                    ClickAction = FirebaseFlutterPushHelper.DefaultClickAction,
                    Data = FirebaseFlutterPushHelper.MergeData(new Dictionary<string, string>
                    {
                        { "type", sourceAction ?? "user_account" },
                        { "sourceAction", sourceAction ?? "user_account" },
                        { "link", link ?? "" },
                        { "notificationId", notificationId?.ToString() ?? "" }
                    })
                };
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{_apiGatewayBaseUrl}/api/firebaseNotification/send-notification", content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Firebase push sent to UserID {UserId} (Company {CompanyId}).", userId, companyId);
                    return true;
                }
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Firebase push failed UserID {UserId}: {Status} {Error}", userId, response.StatusCode, err);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firebase push error UserID {UserId}", userId);
                return false;
            }
        }
    }
}
