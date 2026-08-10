using System.Text.Json;
using System.Text.Json.Serialization;

namespace Digi.Core.AI.Contracts
{
    // ─────────────────────────────────────────────────────────────────────────
    // POST {base}/recruitment/jobreq/generate — the "Generate Job Description
    // with AI" button, mirrored exactly.
    //
    // The response maps 1:1 onto the portal's 4-step job requisition wizard,
    // which is why it is modelled as four steps here rather than flattened: the
    // shape IS the contract, and flattening it would lose which field belongs to
    // which screen.
    //
    // Nullability is load-bearing and NOT laziness. Several fields are null BY
    // DESIGN because they belong to a human, and the service refuses to invent
    // them. See NullByDesignFields for the list and the reasoning.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Request body. Only <see cref="JobTitle"/> is required.</summary>
    public sealed class JobRequisitionRequest
    {
        [JsonPropertyName("companyId")] public int CompanyId { get; set; }

        /// <summary>The one required field. Everything else may be omitted.</summary>
        [JsonPropertyName("jobTitle")] public string JobTitle { get; set; } = string.Empty;

        [JsonPropertyName("department")] public string? Department { get; set; }
        [JsonPropertyName("designation")] public string? Designation { get; set; }

        /// <summary>
        /// Free text — "3 - 6 years", "1-2", "5+ years", "Fresh" all parse. When
        /// supplied, the service uses THESE numbers and will not contradict a
        /// value a human typed. Omit it and the model derives a sensible range.
        /// </summary>
        [JsonPropertyName("experienceRequired")] public string? ExperienceRequired { get; set; }

        /// <summary>
        /// Accepts the messy prose recruiters actually type. The service
        /// normalises it, dropping conjunctions and hedge words while preserving
        /// compound tokens (.NET, C#, CI/CD, Node.js) and the recruiter's casing.
        /// </summary>
        [JsonPropertyName("keySkills")] public string? KeySkills { get; set; }

        /// <summary>
        /// The dropdown's allowed values. Send these and the service snaps its
        /// answer to a real option so it always binds, or returns null when
        /// genuinely ambiguous. Omit them and you get free text the dropdown may
        /// reject.
        /// </summary>
        [JsonPropertyName("jobCategoryOptions")] public List<string>? JobCategoryOptions { get; set; }

        /// <summary>Free-form: any object, a string, or null. Anything else the portal knows.</summary>
        [JsonPropertyName("additional_context")] public object? AdditionalContext { get; set; }
    }

    /// <summary>Wizard step 1 — Basic Information.</summary>
    public sealed class JobRequisitionBasicInfo
    {
        // Verbatim echoes of what we sent, so they always bind back to the
        // dropdowns without a lookup.
        [JsonPropertyName("job_title")] public string? JobTitle { get; set; }
        [JsonPropertyName("department")] public string? Department { get; set; }
        [JsonPropertyName("designation")] public string? Designation { get; set; }

        [JsonPropertyName("job_summary")] public string? JobSummary { get; set; }
        [JsonPropertyName("job_category")] public string? JobCategory { get; set; }

        /// <summary>Always 1 — a starting value for the human to change.</summary>
        [JsonPropertyName("vacancies")] public int? Vacancies { get; set; }

        /// <summary>Null by design — HR's decision.</summary>
        [JsonPropertyName("employment_type")] public string? EmploymentType { get; set; }

        /// <summary>Null by design — HR's decision.</summary>
        [JsonPropertyName("grade")] public string? Grade { get; set; }
    }

    public sealed class JobRequisitionRange
    {
        [JsonPropertyName("minimum")] public int? Minimum { get; set; }
        [JsonPropertyName("maximum")] public int? Maximum { get; set; }

        public bool HasValue => Minimum.HasValue || Maximum.HasValue;
    }

    /// <summary>Wizard step 2 — Requirements.</summary>
    public sealed class JobRequisitionRequirements
    {
        [JsonPropertyName("experience_years")] public JobRequisitionRange? ExperienceYears { get; set; }

        /// <summary>
        /// ALWAYS null, and must stay that way. Age is a protected attribute; an
        /// AI proposing an age band in a job advert is discriminatory and, under
        /// the EU AI Act's high-risk hiring rules, indefensible. The service
        /// enforces this and the portal must never backfill it.
        /// </summary>
        [JsonPropertyName("age_limits")] public JobRequisitionRange? AgeLimits { get; set; }

        [JsonPropertyName("key_responsibilities")] public List<string> KeyResponsibilities { get; set; } = new();
        [JsonPropertyName("requirements")] public List<string> Requirements { get; set; } = new();
        [JsonPropertyName("qualifications")] public List<string> Qualifications { get; set; } = new();
        [JsonPropertyName("skills")] public List<string> Skills { get; set; } = new();
    }

    /// <summary>Wizard step 3 — Compensation. Everything except location is HR's.</summary>
    public sealed class JobRequisitionCompensation
    {
        [JsonPropertyName("location")] public string? Location { get; set; }

        /// <summary>Null by design.</summary>
        [JsonPropertyName("benefits")] public string? Benefits { get; set; }

        /// <summary>Null by design.</summary>
        [JsonPropertyName("budget_type")] public string? BudgetType { get; set; }

        /// <summary>Null by design.</summary>
        [JsonPropertyName("budget_line_id")] public int? BudgetLineId { get; set; }
    }

    /// <summary>Wizard step 4 — Publishing. The AI never publishes.</summary>
    public sealed class JobRequisitionPublishing
    {
        /// <summary>Null by design.</summary>
        [JsonPropertyName("justification")] public string? Justification { get; set; }

        /// <summary>Always false — a human publishes.</summary>
        [JsonPropertyName("is_public_job")] public bool? IsPublicJob { get; set; }

        /// <summary>Always "Draft".</summary>
        [JsonPropertyName("status")] public string? Status { get; set; }

        /// <summary>Null by design. Kept as a string: date formats vary and this is never parsed.</summary>
        [JsonPropertyName("closing_date")] public string? ClosingDate { get; set; }
    }

    public sealed class JobRequisitionData
    {
        [JsonPropertyName("step_1_basic_info")] public JobRequisitionBasicInfo? BasicInfo { get; set; }
        [JsonPropertyName("step_2_requirements")] public JobRequisitionRequirements? Requirements { get; set; }
        [JsonPropertyName("step_3_compensation")] public JobRequisitionCompensation? Compensation { get; set; }
        [JsonPropertyName("step_4_publishing")] public JobRequisitionPublishing? Publishing { get; set; }
    }

    /// <summary>
    /// Generation metadata. Additive by contract — the service gains keys as it
    /// evolves — so unknown members are captured rather than causing a
    /// deserialization failure.
    /// </summary>
    public sealed class JobRequisitionMeta
    {
        [JsonPropertyName("service_version")] public string? ServiceVersion { get; set; }

        /// <summary>
        /// True when the deterministic server-side cache answered. Worth logging:
        /// it is the difference between a 9 ms reply and a 30 s one, and someone
        /// will eventually ask why a call was slow.
        /// </summary>
        [JsonPropertyName("cache_hit")] public bool? CacheHit { get; set; }

        /// <summary>"parsed_from_request" when the service used the numbers we sent.</summary>
        [JsonPropertyName("experience_source")] public string? ExperienceSource { get; set; }

        /// <summary>"selected_from_options" when the answer snapped to our dropdown values.</summary>
        [JsonPropertyName("job_category_source")] public string? JobCategorySource { get; set; }

        [JsonPropertyName("work_mode")] public string? WorkMode { get; set; }

        /// <summary>Shape owned by the service; passed through untouched.</summary>
        [JsonPropertyName("repairs")] public JsonElement? Repairs { get; set; }

        [JsonExtensionData] public Dictionary<string, JsonElement>? Additional { get; set; }
    }

    /// <summary>Envelope of POST {base}/recruitment/jobreq/generate.</summary>
    public sealed class JobRequisitionResult
    {
        [JsonPropertyName("status")] public string? Status { get; set; }

        /// <summary>
        /// Bound as long, never a floating-point type. The service returns an
        /// integer here and System.Text.Json throws on a float-to-int bind —
        /// this was a real defect once and is called out in the contract.
        /// </summary>
        [JsonPropertyName("execution_time_ms")] public long? ExecutionTimeMs { get; set; }

        [JsonPropertyName("companyId")] public int? CompanyId { get; set; }

        /// <summary>
        /// Always true. Drives the "AI-generated — please review" affordance.
        /// Never design around it: a human edits and approves, always.
        /// </summary>
        [JsonPropertyName("review_required")] public bool ReviewRequired { get; set; } = true;

        [JsonPropertyName("data")] public JobRequisitionData? Data { get; set; }
        [JsonPropertyName("meta")] public JobRequisitionMeta? Meta { get; set; }
    }

    /// <summary>
    /// The fields the AI deliberately leaves empty because they are a human's to
    /// decide. Named here so the portal can show them as "for you to complete"
    /// rather than looking like the generation failed — and so nobody
    /// "helpfully" backfills them with defaults later.
    /// </summary>
    public static class NullByDesignFields
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            "age_limits", "benefits", "justification", "employment_type",
            "grade", "budget_type", "budget_line_id", "closing_date"
        };
    }
}
