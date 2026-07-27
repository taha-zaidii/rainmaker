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

        /// <summary>GET /health — liveness only. Open endpoint, no key needed.</summary>
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
