using System.Text.RegularExpressions;

namespace Digi.Core.AI.Configuration
{
    /// <summary>
    /// The single source of truth for every path on Multinet's in-house AI
    /// service, and for turning whatever an HR user typed into the settings page
    /// into a base URL the client can actually compose against.
    ///
    /// Two rules make this class necessary rather than a handful of string
    /// literals scattered through the service:
    ///
    /// 1. The settings page stores ONE "API Endpoint" but the service exposes
    ///    several feature endpoints. That field therefore holds the BASE URL and
    ///    the backend appends the per-feature path. Every path lives here so the
    ///    set can be reviewed against the contract in one glance.
    /// 2. A path that is wrong is a 404 forty seconds into a recruiter's day.
    ///    Guessing one is explicitly forbidden by the integration contract, and
    ///    a guess has already cost this project real debugging time — see
    ///    <see cref="LegacyQuerySegment"/>.
    /// </summary>
    public static class MultinetAiEndpoints
    {
        // ── Feature paths ────────────────────────────────────────────────────
        //
        // Deliberately RELATIVE and without a leading slash. HttpClient composes
        // a relative Uri against BaseAddress only when the base ends in "/" and
        // the relative part does not begin with one; a leading slash would throw
        // away the base's path and turn
        //   https://ai.rainmaker.pk/hrms/api/v1/ + auth/verify
        // into
        //   https://ai.rainmaker.pk/auth/verify
        // which is a silent 404. Keep them exactly as written.

        /// <summary>GET — validates the API key. Zero GPU cost, millisecond response.</summary>
        public const string VerifyKey = "auth/verify";

        /// <summary>POST — the "Generate Job Description with AI" button. Maps onto the 4-step wizard.</summary>
        public const string GenerateJobRequisition = "recruitment/jobreq/generate";

        /// <summary>POST multipart — resume parsing. Form field name is <c>file</c>.</summary>
        public const string ExtractResume = "parser/extract";

        /// <summary>POST JSON — URL-based resume parsing. The portal's primary flow.</summary>
        public const string ExtractResumeByUrl = "parser/extract-url";

        /// <summary>POST — candidate screening against a requisition. Honours the Auto Shortlist Threshold.</summary>
        public const string ScreenCandidate = "recruitment/screening/screen";

        /// <summary>POST — embeddings-based ranking for the RANK column. Fast, no GPU lock.</summary>
        public const string RankCandidates = "matching/rank";

        /// <summary>POST — interview question bank, grouped by category.</summary>
        public const string InterviewQuestions = "recruitment/interview/questions";

        /// <summary>POST — rubric-governed candidate evaluation assist. ~60 s.</summary>
        public const string ScoreCandidate = "scoring/score";

        /// <summary>
        /// Capability slugs reported by <see cref="VerifyKey"/>. Feature toggles
        /// should be enabled from this list rather than hard-coded, so the portal
        /// follows the service as it gains features instead of needing a deploy.
        /// </summary>
        public static class Capabilities
        {
            public const string ParserExtract = "parser.extract";
            public const string JobRequisitionGenerate = "recruitment.jobreq.generate";
            public const string ScreeningScreen = "recruitment.screening.screen";
            public const string InterviewQuestions = "recruitment.interview.questions";
            public const string MatchingRank = "matching.rank";
            public const string ScoringScore = "scoring.score";
        }

        /// <summary>
        /// Maps a feature path to the capability that gates it, so a caller can
        /// ask "may I call this?" without restating the pairing at each site.
        /// </summary>
        public static string? CapabilityFor(string featurePath) => featurePath switch
        {
            ExtractResume or ExtractResumeByUrl => Capabilities.ParserExtract,
            GenerateJobRequisition => Capabilities.JobRequisitionGenerate,
            ScreenCandidate => Capabilities.ScreeningScreen,
            InterviewQuestions => Capabilities.InterviewQuestions,
            RankCandidates => Capabilities.MatchingRank,
            ScoreCandidate => Capabilities.ScoringScore,

            // auth/verify is how capabilities are discovered, so it cannot itself
            // be gated on one.
            _ => null
        };

        // ── Base URL resolution ──────────────────────────────────────────────

        /// <summary>
        /// The path segment of the endpoint that the settings page's own helper
        /// text used to recommend. There is no such endpoint — it returns 404.
        /// It is still sitting in live tenant configuration, so we detect it.
        /// </summary>
        private const string LegacyQuerySegment = "query";

        /// <summary>Recognises a version segment such as <c>v1</c> or <c>v2</c>.</summary>
        private static readonly Regex VersionSegment =
            new(@"^v\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Outcome of interpreting a configured endpoint.
        ///
        /// <paramref name="Problem"/> being non-null means the value is unusable
        /// and no call should be attempted. <paramref name="Warning"/> means the
        /// value is usable but suspicious — surface it in the settings UI rather
        /// than failing, because the AI service may legitimately move.
        /// </summary>
        /// <param name="BaseUri">Normalised, always ending in "/". Null when <paramref name="Problem"/> is set.</param>
        /// <param name="Problem">Why the value cannot be used, phrased for whoever configures the portal.</param>
        /// <param name="Warning">A usable value that still looks wrong.</param>
        /// <param name="WasCorrected">True when the stored value was rewritten; show this so the tenant fixes it at source.</param>
        public sealed record BaseUrlResolution(
            Uri? BaseUri,
            string? Problem = null,
            string? Warning = null,
            bool WasCorrected = false)
        {
            public bool IsUsable => BaseUri is not null;
        }

        /// <summary>
        /// Turns a configured "API Endpoint" into a base URI suitable for
        /// <see cref="HttpClient.BaseAddress"/>.
        ///
        /// This is forgiving on purpose. The field is edited by HR administrators
        /// through a web form, the portal's own helper text recommended a wrong
        /// value for months, and the cost of being strict is a recruiter seeing
        /// "AI unavailable" with no way to self-diagnose. So: fix what is
        /// unambiguously fixable, warn about what is merely suspicious, and
        /// refuse only what cannot be interpreted at all.
        /// </summary>
        public static BaseUrlResolution ResolveBaseUrl(string? configuredEndpoint)
        {
            if (string.IsNullOrWhiteSpace(configuredEndpoint))
            {
                return new BaseUrlResolution(
                    null,
                    Problem: "No API Endpoint is configured. Set it to the AI service base URL, " +
                             "for example https://ai.rainmaker.pk/hrms/api/v1");
            }

            var trimmed = configuredEndpoint.Trim();

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return new BaseUrlResolution(
                    null,
                    Problem: $"The API Endpoint '{trimmed}' is not an absolute http/https URL. " +
                             "Enter the base URL only, for example https://ai.rainmaker.pk/hrms/api/v1");
            }

            // A query string or fragment on a base URL is always a mistake and
            // would corrupt every composed path, so drop them rather than
            // producing ".../auth/verify?foo=bar".
            var hadQueryOrFragment = uri.Query.Length > 0 || uri.Fragment.Length > 0;

            var segments = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var corrected = false;
            string? warning = null;

            // The known-bad value: ".../api/query". The helper text on the
            // settings page recommended it, it 404s, and it is still stored for
            // at least one live tenant. The intent is unambiguous — that
            // position holds the API version — so repair it rather than letting
            // a recruiter hit a 404. Narrowly scoped: only when the final
            // segment is exactly "query" and it directly follows "api".
            if (segments.Count >= 2 &&
                segments[^1].Equals(LegacyQuerySegment, StringComparison.OrdinalIgnoreCase) &&
                segments[^2].Equals("api", StringComparison.OrdinalIgnoreCase))
            {
                segments[^1] = "v1";
                corrected = true;
                warning =
                    $"The stored API Endpoint ended in '/{LegacyQuerySegment}', which does not exist on the " +
                    "AI service and returns 404. It has been treated as '/v1' for this call. " +
                    "Please correct the saved value to the base URL, e.g. https://ai.rainmaker.pk/hrms/api/v1";
            }
            else if (segments.Count == 0 || !VersionSegment.IsMatch(segments[^1]))
            {
                // Usable — the service could be hosted anywhere — but the
                // documented base carries an explicit API version, and omitting
                // it is the most likely reason for a 404 on every feature.
                warning =
                    $"The API Endpoint '{trimmed}' does not end in an API version segment such as '/v1'. " +
                    "The backend appends feature paths to this value, so the base URL should be the " +
                    "versioned root, e.g. https://ai.rainmaker.pk/hrms/api/v1";
            }

            if (hadQueryOrFragment)
            {
                corrected = true;
                warning ??= "The API Endpoint contained a query string or fragment, which has been " +
                            "ignored. Store the base URL only.";
            }

            // Trailing slash is load-bearing for BaseAddress composition — see
            // the note on the path constants above.
            var builder = new UriBuilder(uri)
            {
                Path = segments.Count == 0 ? "/" : "/" + string.Join('/', segments) + "/",
                Query = string.Empty,
                Fragment = string.Empty
            };

            return new BaseUrlResolution(builder.Uri, Problem: null, Warning: warning, WasCorrected: corrected);
        }

        /// <summary>
        /// Composes a feature path onto a resolved base. Kept here so the
        /// leading/trailing slash rule is applied in exactly one place.
        /// </summary>
        public static Uri Combine(Uri baseUri, string featurePath)
        {
            ArgumentNullException.ThrowIfNull(baseUri);
            return new Uri(baseUri, featurePath.TrimStart('/'));
        }
    }
}
