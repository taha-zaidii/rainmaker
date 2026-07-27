namespace Digi.Shared.Services
{
    public static class SessionRevocationConstants
    {
        /// <summary>SignalR event name — Angular/mobile must listen and logout immediately.</summary>
        public const string ForceLogoutEvent = "ForceLogout";

        /// <summary>FCM data payload type / sourceAction for silent or visible logout.</summary>
        public const string SessionRevokedAction = "SESSION_REVOKED";

        public const string ReasonPasswordChanged = "PASSWORD_CHANGED";
    }
}
