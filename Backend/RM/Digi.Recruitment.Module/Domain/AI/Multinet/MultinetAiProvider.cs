namespace Digi.Recruitment.Module.Domain.AI.Multinet
{
    /// <summary>
    /// Identifies the provider key for Multinet's in-house AI service.
    ///
    /// Recognition is by PROVIDER NAME ONLY, never by inspecting the configured
    /// endpoint. That restraint is deliberate: <c>custom</c> is the portal's
    /// escape hatch for third-party services a client brings themselves — Groq,
    /// DeepSeek, a self-hosted gateway — and it must keep meaning exactly that.
    /// Sniffing a URL to decide "this looks like ours, I will handle it" would
    /// hijack a client's own configuration the moment their chosen service
    /// happened to sit behind a familiar-looking host, and the resulting bug
    /// would be invisible in the settings UI.
    ///
    /// The dropdown says what the client chose. That is the whole signal.
    /// </summary>
    public static class MultinetAiProvider
    {
        /// <summary>Canonical provider key. Stored lowercase, like the other providers.</summary>
        public const string Name = "multinetai";

        /// <summary>How the settings page should label it.</summary>
        public const string DisplayName = "MultinetAI";

        /// <summary>True when the company selected Multinet's in-house AI service.</summary>
        public static bool Matches(string? provider) =>
            !string.IsNullOrWhiteSpace(provider) &&
            provider.Trim().Equals(Name, StringComparison.OrdinalIgnoreCase);
    }
}
