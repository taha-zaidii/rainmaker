using System.Linq;

namespace Digi.Shared.Helper
{
    /// <summary>
    /// Loose validation for FCM registration tokens (Android/iOS/Web). Treat as opaque strings; reject obvious garbage.
    /// </summary>
    public static class FcmRegistrationTokenValidator
    {
        public const int MinLength = 32;
        public const int MaxLength = 4096;

        /// <summary>Returns null when valid; otherwise a short error message.</summary>
        public static string? GetValidationError(string? deviceToken)
        {
            if (string.IsNullOrWhiteSpace(deviceToken))
                return "Device token is required.";

            var t = deviceToken.Trim();
            if (t.Length < MinLength || t.Length > MaxLength)
                return $"Device token length must be between {MinLength} and {MaxLength} characters.";

            // FCM tokens are typically base64url-like plus ':' (e.g. instance id prefixes). Reject control chars / whitespace.
            if (t.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
                return "Device token contains invalid characters.";

            return null;
        }

        public static bool IsValid(string? deviceToken) => GetValidationError(deviceToken) == null;
    }
}
