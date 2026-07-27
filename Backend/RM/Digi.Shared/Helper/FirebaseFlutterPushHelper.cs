using Digi.Shared.DTOs.notification;

namespace Digi.Shared.Helper
{
    /// <summary>
    /// Default FCM fields required by the Rainmaker Flutter app (system tray + tap handler).
    /// </summary>
    public static class FirebaseFlutterPushHelper
    {
        public const string DefaultChannelId = "high_importance_channel";
        public const string DefaultClickAction = "FLUTTER_NOTIFICATION_CLICK";

        public const string DataKeyChannelId = "channel_id";
        public const string DataKeyClickAction = "click_action";

        public static string ResolveChannelId(string? requestChannelId) =>
            string.IsNullOrWhiteSpace(requestChannelId) ? DefaultChannelId : requestChannelId.Trim();

        public static string ResolveClickAction(string? requestClickAction) =>
            string.IsNullOrWhiteSpace(requestClickAction) ? DefaultClickAction : requestClickAction.Trim();

        /// <summary>
        /// Ensures Flutter/Android data payload includes channel_id and click_action (all values are strings).
        /// </summary>
        public static Dictionary<string, string> MergeData(Dictionary<string, string>? data)
        {
            var merged = data != null
                ? new Dictionary<string, string>(data, StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal);

            merged[DataKeyChannelId] = ResolveChannelId(
                merged.TryGetValue(DataKeyChannelId, out var existingChannel) ? existingChannel : null);

            merged[DataKeyClickAction] = ResolveClickAction(
                merged.TryGetValue(DataKeyClickAction, out var existingClick) ? existingClick : null);

            return merged;
        }

        /// <summary>
        /// FCM request for HR / background processor — high priority + Flutter tray channel/click_action.
        /// </summary>
        public static SendFirebaseNotificationRequestDto CreateHrPushRequest(
            int companyId,
            int userId,
            string? title,
            string? body,
            int notificationId,
            string? link = null)
        {
            return new SendFirebaseNotificationRequestDto
            {
                CompanyID = companyId,
                UserID = userId,
                Title = title ?? "Notification",
                Body = body ?? "",
                Priority = 2,
                Sound = "default",
                ChannelId = DefaultChannelId,
                ClickAction = DefaultClickAction,
                Data = MergeData(new Dictionary<string, string>
                {
                    { "notificationId", notificationId.ToString() },
                    { "link", link ?? "" },
                    { "type", "hr_notification" }
                })
            };
        }
    }
}
