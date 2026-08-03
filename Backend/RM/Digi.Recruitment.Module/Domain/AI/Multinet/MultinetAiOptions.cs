using System.ComponentModel.DataAnnotations;

namespace Digi.Recruitment.Module.Domain.AI.Multinet
{
    /// <summary>
    /// Configuration for Multinet's in-house AI service (hrms-ai-service).
    ///
    /// This is a metered, API-key-authenticated service Multinet runs on its own
    /// GPUs — the same shape as the OpenAI / Anthropic / Google integrations the
    /// module already supports, but ours, and sold as an add-on across products
    /// (HRMS, LMS, CRM). Treat the base URL as pure configuration: it is
    /// loopback in dev and an internal hostname behind nginx in production.
    /// </summary>
    public sealed class MultinetAiOptions
    {
        public const string SectionName = "MultinetAI";

        /// <summary>
        /// Versioned root of the AI service — the BASE URL only. The backend
        /// appends each feature path (auth/verify, recruitment/jobreq/generate,
        /// parser/extract …), so this must be the versioned root and nothing
        /// deeper. A trailing slash is optional; it is normalised either way.
        ///
        /// Note this is only the fallback. The portal is multi-tenant and each
        /// company stores its own endpoint in Tbl_Ruc_RecruitmentAI_Settings,
        /// which overrides this per call.
        /// </summary>
        [Required]
        public string BaseUrl { get; set; } = "https://ai.rainmaker.pk/hrms/api/v1";

        /// <summary>
        /// Platform-level key sent as <c>X-API-Key</c>. Never commit a real value:
        /// it belongs in appsettings.Development.json, user-secrets, or an
        /// environment variable. Individual companies may override this with
        /// their own metered key held in Tbl_Ruc_RecruitmentAI_Settings.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Per-request timeout. The contract states a parse takes 40–90 s and
        /// requires a client timeout of at least 180 s; scoring adds ~60 s on the
        /// same GPU lock, so the default leaves headroom above both.
        /// </summary>
        [Range(180, 900)]
        public int TimeoutSeconds { get; set; } = 240;

        /// <summary>
        /// Retries for transient failures only (5xx, timeouts, connection
        /// errors). A 422 is a verdict about the document and is never retried.
        /// </summary>
        [Range(0, 5)]
        public int MaxRetries { get; set; } = 2;

        /// <summary>Upload ceiling enforced before we spend a network round trip. Mirrors HRMS_MAX_UPLOAD_MB.</summary>
        [Range(1, 100)]
        public int MaxUploadMegabytes { get; set; } = 20;

        /// <summary>Extensions the service accepts. Anything else is rejected locally.</summary>
        public string[] AllowedExtensions { get; set; } = { ".pdf", ".docx", ".png", ".jpg", ".jpeg" };

        /// <summary>
        /// Serve canned, contract-shaped responses instead of calling the real
        /// service. This exists so frontend and backend work never blocks on a
        /// busy GPU, and so demos degrade gracefully when the service is down.
        /// Must never be true in production.
        /// </summary>
        public bool StubMode { get; set; }

        /// <summary>
        /// Artificial delay for stubbed parses, so the UI's long-running states
        /// (progress, polling, cancel) are exercised in development instead of
        /// completing instantly and hiding a frozen-looking screen in production.
        /// </summary>
        [Range(0, 120)]
        public int StubLatencySeconds { get; set; } = 3;

        /// <summary>Master switch for the provider. When false the module behaves as if the in-house service does not exist.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gate parse submissions on GET /ready before queuing work. Turning this
        /// off lets jobs be submitted straight into a cold service.
        /// </summary>
        public bool GateOnReadiness { get; set; } = true;

        /// <summary>How long a cached /ready result stays fresh, so status polling cannot stampede the probe.</summary>
        [Range(1, 300)]
        public int ReadinessCacheSeconds { get; set; } = 10;

        public long MaxUploadBytes => (long)MaxUploadMegabytes * 1024 * 1024;

        /// <summary>
        /// Fail fast at startup on a misconfiguration, rather than at 2 a.m. on
        /// the first upload. Returns the problems found; empty means valid.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var problems = new List<string>();

            if (!Enabled)
            {
                return problems;
            }

            var resolvedBaseUrl = MultinetAiEndpoints.ResolveBaseUrl(BaseUrl);
            if (!resolvedBaseUrl.IsUsable)
            {
                problems.Add($"{SectionName}:BaseUrl — {resolvedBaseUrl.Problem}");
            }

            // NOTE: a missing platform key is deliberately NOT fatal.
            //
            // This is a multi-tenant portal: the key that actually gets used is
            // the per-company one in Tbl_Ruc_RecruitmentAI_Settings, entered
            // through the AI Settings screen and stored encrypted. The value
            // here is only a fallback for callers with no company context.
            //
            // Failing startup because the fallback is unset would block the
            // normal, correct configuration — every tenant holding its own key
            // and the platform holding none.

            if (TimeoutSeconds < 180)
            {
                problems.Add(
                    $"{SectionName}:TimeoutSeconds is {TimeoutSeconds}. A parse legitimately takes " +
                    "40–90 s and the contract requires at least 180 s.");
            }

            if (AllowedExtensions is null || AllowedExtensions.Length == 0)
            {
                problems.Add($"{SectionName}:AllowedExtensions must list at least one extension.");
            }

            return problems;
        }
    }
}
