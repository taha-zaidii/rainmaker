using System.Text.Json;
using System.Text.Json.Serialization;

namespace Digi.Recruitment.Module.Domain.AI.Multinet
{
    // ─────────────────────────────────────────────────────────────────────────
    // The frozen wire contract of hrms-ai-service, mirrored exactly.
    //
    // ProfileSchema is versioned (currently 1.2.0) and owned by the AI service —
    // these types are a mirror, not a source of truth. If schema_version comes
    // back as something we do not recognise we surface it rather than guessing,
    // because a silently-shifted contract corrupts candidate records.
    //
    // Nullability here is deliberate and load-bearing: sparse resumes are valid
    // documents, so almost everything can legitimately be null or empty.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Schema versions this build has been written and tested against.</summary>
    public static class ProfileSchemaVersions
    {
        public const string Supported = "1.2.0";

        /// <summary>Same major.minor is compatible; a patch bump cannot change shape.</summary>
        public static bool IsCompatible(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            var actual = version.Split('.');
            var expected = Supported.Split('.');
            return actual.Length >= 2
                   && actual[0] == expected[0]
                   && actual[1] == expected[1];
        }
    }

    public sealed class EducationNode
    {
        [JsonPropertyName("institution")] public string Institution { get; set; } = string.Empty;
        [JsonPropertyName("degree")] public string Degree { get; set; } = string.Empty;
        [JsonPropertyName("duration")] public string? Duration { get; set; }
        [JsonPropertyName("gpa")] public string? Gpa { get; set; }
    }

    public sealed class ExperienceNode
    {
        [JsonPropertyName("company")] public string Company { get; set; } = string.Empty;
        [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        [JsonPropertyName("duration")] public string Duration { get; set; } = string.Empty;
        [JsonPropertyName("location")] public string? Location { get; set; }
        [JsonPropertyName("achievements")] public List<string> Achievements { get; set; } = new();
    }

    public sealed class ProjectNode
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        /// <summary>Comma-joined string in the contract, not an array. Kept as-is.</summary>
        [JsonPropertyName("technologies")] public string? Technologies { get; set; }

        [JsonPropertyName("description")] public List<string> Description { get; set; } = new();
    }

    /// <summary>ProfileSchema v1.2.0 — the parsed candidate profile.</summary>
    public sealed class CandidateProfile
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("location")] public string? Location { get; set; }
        [JsonPropertyName("headline")] public string? Headline { get; set; }
        [JsonPropertyName("summary")] public string? Summary { get; set; }
        [JsonPropertyName("spoken_languages")] public List<string> SpokenLanguages { get; set; } = new();
        [JsonPropertyName("links")] public List<string> Links { get; set; } = new();
        [JsonPropertyName("skills")] public List<string> Skills { get; set; } = new();
        [JsonPropertyName("education")] public List<EducationNode> Education { get; set; } = new();
        [JsonPropertyName("experience")] public List<ExperienceNode> Experience { get; set; } = new();
        [JsonPropertyName("projects")] public List<ProjectNode> Projects { get; set; } = new();

        [JsonPropertyName("certifications_and_awards")]
        public List<string> CertificationsAndAwards { get; set; } = new();
    }

    /// <summary>
    /// Per-field record of HOW a value was obtained. This is the review UI's most
    /// important signal: anything not produced and verified by the model itself
    /// (regex, vision_escalation, llm_unverified …) gets flagged for a human.
    /// </summary>
    public static class FieldProvenance
    {
        /// <summary>Provenance values that mean "the model produced and validated this".</summary>
        private static readonly HashSet<string> Trusted = new(StringComparer.OrdinalIgnoreCase)
        {
            "llm", "llm_verified", "text", "docling", "schema"
        };

        /// <summary>True when a reviewer should look at this field before accepting it.</summary>
        public static bool NeedsReview(string? provenance) =>
            string.IsNullOrWhiteSpace(provenance) || !Trusted.Contains(provenance);
    }

    /// <summary>
    /// Pipeline metadata. Extra keys are expected — the AI service adds timing
    /// fields as it evolves — so unknown members are captured rather than dropped,
    /// and never cause a deserialization failure.
    /// </summary>
    public sealed class ParseMeta
    {
        [JsonPropertyName("schema_version")] public string? SchemaVersion { get; set; }

        /// <summary>text | text+raw_fallback | ocr+vision_hybrid | vision | text+vision_escalated</summary>
        [JsonPropertyName("extraction_route")] public string? ExtractionRoute { get; set; }

        [JsonPropertyName("field_provenance")]
        public Dictionary<string, string> FieldProvenance { get; set; } = new();

        [JsonPropertyName("stage1_docling_ms")] public double? Stage1DoclingMs { get; set; }
        [JsonPropertyName("stage3_ollama_ms")] public double? Stage3OllamaMs { get; set; }
        [JsonPropertyName("total_wall_ms")] public double? TotalWallMs { get; set; }
        [JsonPropertyName("prompt_tokens")] public int? PromptTokens { get; set; }
        [JsonPropertyName("output_tokens")] public int? OutputTokens { get; set; }
        [JsonPropertyName("retries_used")] public int? RetriesUsed { get; set; }
        [JsonPropertyName("docling_coverage")] public double? DoclingCoverage { get; set; }
        [JsonPropertyName("validation_passed")] public bool? ValidationPassed { get; set; }

        /// <summary>Timing and any other fields added after this client was written.</summary>
        [JsonExtensionData] public Dictionary<string, JsonElement>? Additional { get; set; }

        /// <summary>Field names a reviewer should check, derived from provenance.</summary>
        public IReadOnlyCollection<string> FieldsNeedingReview() =>
            FieldProvenance
                .Where(kv => Multinet.FieldProvenance.NeedsReview(kv.Value))
                .Select(kv => kv.Key)
                .ToArray();
    }

    /// <summary>Envelope of POST /api/v1/parser/extract.</summary>
    public sealed class ParseResumeResult
    {
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("data")] public CandidateProfile? Data { get; set; }
        [JsonPropertyName("meta")] public ParseMeta? Meta { get; set; }
    }

    /// <summary>GET /health — liveness only, proves nothing about the GPU.</summary>
    public sealed class ServiceHealth
    {
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("service")] public string? Service { get; set; }
        [JsonPropertyName("version")] public string? Version { get; set; }
    }

    public sealed class LlmBackendStatus
    {
        [JsonPropertyName("reachable")] public bool Reachable { get; set; }
        [JsonPropertyName("version")] public string? Version { get; set; }
    }

    /// <summary>GET /ready — 200 ready / 503 not_ready. Parse submissions gate on this.</summary>
    public sealed class ServiceReadiness
    {
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("llm_backend")] public LlmBackendStatus? LlmBackend { get; set; }
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("backend")] public string? Backend { get; set; }

        public bool IsReady => string.Equals(Status, "ready", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>GET /version.</summary>
    public sealed class ServiceVersion
    {
        [JsonPropertyName("service_version")] public string? ServiceVersion_ { get; set; }
        [JsonPropertyName("schema_version")] public string? SchemaVersion { get; set; }
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("backend")] public string? Backend { get; set; }
    }

    /// <summary>
    /// GET /auth/verify — the only supported reachability probe.
    ///
    /// The ops endpoints (/health, /ready, /version) are deliberately 404 at the
    /// nginx edge and answer only on-box, so this doubles as the portal's health
    /// check. It costs zero GPU and returns in milliseconds, which is why it is
    /// safe to call on every settings save.
    /// </summary>
    public sealed class KeyVerification
    {
        [JsonPropertyName("valid")] public bool Valid { get; set; }
        [JsonPropertyName("service")] public string? Service { get; set; }
        [JsonPropertyName("service_version")] public string? ServiceVersion { get; set; }
        [JsonPropertyName("schema_version")] public string? SchemaVersion { get; set; }

        /// <summary>
        /// What this key is allowed to do, as slugs (see
        /// <see cref="MultinetAiEndpoints.Capabilities"/>). Feature toggles are
        /// driven from this rather than hard-coded, so the portal follows the
        /// service as it gains features instead of needing a redeploy.
        /// </summary>
        [JsonPropertyName("capabilities")] public List<string> Capabilities { get; set; } = new();

        /// <summary>The service adds fields over time; unknown ones are kept, never fatal.</summary>
        [JsonExtensionData] public Dictionary<string, JsonElement>? Additional { get; set; }

        public bool Supports(string capability) =>
            Capabilities.Any(c => string.Equals(c, capability, StringComparison.OrdinalIgnoreCase));
    }

    public sealed class CandidateIndexEntry
    {
        [JsonPropertyName("profile_id")] public string ProfileId { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    /// <summary>GET /api/v1/candidates — what the AI service itself has parsed and indexed.</summary>
    public sealed class CandidateIndexResult
    {
        [JsonPropertyName("count")] public int Count { get; set; }
        [JsonPropertyName("candidates")] public List<CandidateIndexEntry> Candidates { get; set; } = new();
    }

    /// <summary>POST /api/v1/matching/rank — embeddings-based, fast, no GPU lock.</summary>
    public sealed class RankResult
    {
        [JsonPropertyName("model_version")] public string? ModelVersion { get; set; }

        [JsonPropertyName("section_weights")]
        public Dictionary<string, double> SectionWeights { get; set; } = new();

        /// <summary>Shape is owned by the matching backend; passed through untouched.</summary>
        [JsonPropertyName("ranking")] public List<JsonElement> Ranking { get; set; } = new();
    }

    /// <summary>
    /// POST /api/v1/scoring/score — rubric-governed, ~60 s, shares the GPU lock.
    /// Free-form because the rubric is still a placeholder; the one field we do
    /// depend on is rubric_signed_off, which decides whether the UI may present
    /// the score as anything other than advisory.
    /// </summary>
    public sealed class ScoreResult
    {
        [JsonPropertyName("rubric_signed_off")] public bool? RubricSignedOff { get; set; }
        [JsonPropertyName("candidate")] public string? Candidate { get; set; }
        [JsonExtensionData] public Dictionary<string, JsonElement>? Payload { get; set; }

        /// <summary>Until HR signs the rubric off, every score is advisory. Absent flag is treated as not signed off.</summary>
        public bool IsAdvisoryOnly => RubricSignedOff != true;
    }

    /// <summary>
    /// Error body shape: <c>{"detail": {"error": code, "message": text}}</c>.
    /// One path (400, no filename) returns <c>detail</c> as a bare string, so
    /// both forms have to be tolerated.
    /// </summary>
    internal sealed class ServiceErrorEnvelope
    {
        [JsonPropertyName("detail")] public JsonElement Detail { get; set; }
    }
}
