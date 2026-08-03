namespace Digi.Recruitment.Module.Domain.AI.Multinet
{
    /// <summary>
    /// Typed client for Multinet's in-house AI service (hrms-ai-service).
    ///
    /// Deliberately NOT modelled as the module's existing prompt-in/text-out
    /// provider interface: this service is a purpose-built resume pipeline with
    /// multipart uploads, a single-flight GPU lock and 40–90 s calls, not a chat
    /// completions endpoint. Forcing it into that shape would lose the schema,
    /// the provenance metadata and the readiness gate — the three things that
    /// make it better than a general-purpose model for this job.
    ///
    /// Every method returns <see cref="AiResult{T}"/>: expected failures are
    /// values, and only genuine programming errors throw.
    ///
    /// <paramref name="apiKey"/> overrides the platform key on any call, which is
    /// how a company's own metered key is used when one is configured for it.
    /// </summary>
    public interface IMultinetAiClient
    {
        /// <summary>True when this client talks to a real service rather than serving canned data.</summary>
        bool IsStub { get; }

        /// <summary>
        /// GET auth/verify — validates the API key and reports what it may do.
        ///
        /// This is the ONLY reachability probe that works in production: the ops
        /// endpoints below are 404 at the nginx edge. It costs zero GPU and
        /// returns in milliseconds, so it is safe on every settings save.
        ///
        /// A wrong key surfaces as an <see cref="AiErrorCode.Unauthorized"/>
        /// failure, never as an unreachable service — callers must be able to
        /// tell a recruiter which of the two went wrong.
        /// </summary>
        /// <param name="baseUriOverride">
        /// Per-company base URL from Tbl_Ruc_RecruitmentAI_Settings. The portal is
        /// multi-tenant and stores an endpoint per company, so the configured
        /// <see cref="MultinetAiOptions.BaseUrl"/> is only the fallback. Resolve
        /// it with <see cref="MultinetAiEndpoints.ResolveBaseUrl"/> first.
        /// </param>
        Task<AiResult<KeyVerification>> VerifyKeyAsync(
            string? apiKey = null,
            Uri? baseUriOverride = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// POST recruitment/jobreq/generate — the "Generate Job Description with
        /// AI" button. Returns a draft that maps 1:1 onto the 4-step requisition
        /// wizard, pre-filled and fully editable by the HR user.
        ///
        /// Takes ~13 s warm and up to ~35 s cold, so the UI needs a spinner that
        /// tolerates the wait. An identical repeat is served from the service's
        /// deterministic cache in milliseconds.
        ///
        /// The result is ADVISORY. It always carries
        /// <see cref="JobRequisitionResult.ReviewRequired"/>, its status is always
        /// Draft, and several fields are null by design because they belong to a
        /// human — see <see cref="NullByDesignFields"/>. Never auto-commit it.
        /// </summary>
        Task<AiResult<JobRequisitionResult>> GenerateJobRequisitionAsync(
            JobRequisitionRequest request,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            CancellationToken cancellationToken = default);

        /// <summary>GET /health — liveness only. On-box only; 404 at the edge.</summary>
        Task<AiResult<ServiceHealth>> GetHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// GET /ready — verifies the LLM backend actually answers. Parse
        /// submissions gate on this; a 503 means park the work, not fail it.
        /// Open endpoint, no key needed.
        /// </summary>
        Task<AiResult<ServiceReadiness>> GetReadinessAsync(CancellationToken cancellationToken = default);

        /// <summary>GET /version — service, schema, model and backend versions. Open endpoint.</summary>
        Task<AiResult<ServiceVersion>> GetVersionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v1/parser/extract — the core integration. One resume in,
        /// a validated ProfileSchema plus provenance metadata out. Takes 40–90 s
        /// and holds the service's GPU lock, so callers must never fan this out.
        /// </summary>
        Task<AiResult<ParseResumeResult>> ExtractResumeAsync(
            Stream content,
            string fileName,
            string? apiKey = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v1/parser/extract-url — URL-based resume parsing.
        /// The portal sends the URL of a resume already stored in file storage.
        /// </summary>
        Task<AiResult<ParseResumeResult>> ExtractResumeByUrlAsync(
            string documentUrl,
            string? candidateId = null,
            string? applicationId = null,
            string? requisitionId = null,
            string? companyId = null,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            CancellationToken cancellationToken = default);

        /// <summary>GET /api/v1/candidates — the corpus the AI service itself holds.</summary>
        Task<AiResult<CandidateIndexResult>> ListCandidatesAsync(
            string? apiKey = null,
            CancellationToken cancellationToken = default);

        /// <summary>POST /api/v1/matching/rank — embeddings-based ranking. Fast, no GPU lock.</summary>
        Task<AiResult<RankResult>> RankAsync(
            string jobDescription,
            int topK,
            string? apiKey = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v1/scoring/score — rubric-governed scoring of one candidate,
        /// ~60 s, shares the GPU lock with parsing. Results are advisory until
        /// <see cref="ScoreResult.RubricSignedOff"/> is true.
        /// </summary>
        Task<AiResult<ScoreResult>> ScoreAsync(
            string profileId,
            string jobDescription,
            string? apiKey = null,
            CancellationToken cancellationToken = default);
    }
}
