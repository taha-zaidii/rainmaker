namespace Digi.Core.AI.Contracts
{
    /// <summary>
    /// Every way a call to the in-house AI service can fail, as a domain concept
    /// rather than an HTTP status. Controllers and the queue worker branch on
    /// this; nothing upstream should have to know the wire protocol.
    /// </summary>
    public enum AiErrorCode
    {
        None = 0,

        /// <summary>401 — key missing, wrong, or revoked. The service is fail-closed.</summary>
        Unauthorized,

        /// <summary>413 — over the upload ceiling. Rejected locally when we can.</summary>
        FileTooLarge,

        /// <summary>422 unsupported_file_type — extension not in the accepted set.</summary>
        UnsupportedFileType,

        /// <summary>422 content_type_mismatch — magic bytes disagree with the extension.</summary>
        ContentTypeMismatch,

        /// <summary>422 extraction_failed — a real document that yielded no valid profile.</summary>
        ExtractionFailed,

        /// <summary>422 file_processing_error — the file could not be read at all.</summary>
        FileProcessingError,

        /// <summary>400 — malformed request (e.g. no filename).</summary>
        BadRequest,

        /// <summary>404 unknown_profile_id — scoring asked for a profile the service has not parsed.</summary>
        UnknownProfileId,

        /// <summary>503 from /ready — the LLM backend is not answering. Park the job, do not fail it.</summary>
        NotReady,

        /// <summary>
        /// 429 busy — the GPU is saturated. Distinct from <see cref="NotReady"/>:
        /// the service is healthy and telling us exactly how long to wait, so the
        /// UI can honestly say "AI is busy, retrying…" rather than "unavailable".
        /// </summary>
        Busy,

        /// <summary>Client-side timeout. The GPU is single-flight, so this usually means a queue behind us.</summary>
        Timeout,

        /// <summary>Connection refused / DNS / socket error — the service is not running or not reachable.</summary>
        Unreachable,

        /// <summary>500 — the service logged something it would not tell us about.</summary>
        InternalError,

        /// <summary>A 2xx whose body did not match the frozen contract. Never silently coerced.</summary>
        ContractViolation,

        /// <summary>Refused before leaving this process (e.g. oversized file, disallowed extension, no key configured).</summary>
        RejectedLocally,

        /// <summary>
        /// The feature has no equivalent on this provider. Candidate ranking and
        /// rubric scoring depend on Multinet's own embeddings corpus and rubric
        /// engine; a general-purpose chat model has nothing behind it to serve
        /// either honestly, so it declines rather than approximating a result
        /// that would look real but rest on nothing.
        /// </summary>
        NotSupportedByProvider
    }

    /// <summary>
    /// A failure with everything a caller needs to decide what to do: whether to
    /// retry, whether to park, and what is safe to show a recruiter.
    /// </summary>
    /// <param name="Code">Domain classification.</param>
    /// <param name="Message">Safe for a UI. The service already sanitizes its own messages; we never add internals.</param>
    /// <param name="HttpStatus">Status observed, when there was a response at all.</param>
    /// <param name="Retryable">True only for genuinely transient conditions.</param>
    /// <param name="ServiceErrorCode">The service's own error slug, kept verbatim for logs and support.</param>
    /// <param name="RetryAfter">
    /// How long the service asked us to wait, from the <c>Retry-After</c> header or
    /// a <c>retry_after_s</c> body field. Only a 429 carries this. Honouring it
    /// matters: the GPU is single-flight, so retrying on our own schedule just
    /// lengthens the queue we are already stuck behind.
    /// </param>
    public sealed record AiError(
        AiErrorCode Code,
        string Message,
        int? HttpStatus = null,
        bool Retryable = false,
        string? ServiceErrorCode = null,
        TimeSpan? RetryAfter = null)
    {
        public override string ToString() =>
            HttpStatus is null
                ? $"{Code}: {Message}"
                : $"{Code} (HTTP {HttpStatus}): {Message}";
    }

    /// <summary>
    /// Success-or-failure without exceptions for expected outcomes. A resume the
    /// model cannot read is a normal business result, not an exceptional one, and
    /// exception-driven control flow around a 90-second call is hard to reason about.
    /// </summary>
    public sealed class AiResult<T>
    {
        private AiResult(bool isSuccess, T? value, AiError? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        public T? Value { get; }

        public AiError? Error { get; }

        /// <summary>The value on success; throws if called on a failure — use only after checking IsSuccess.</summary>
        public T ValueOrThrow => IsSuccess && Value is not null
            ? Value
            : throw new InvalidOperationException(
                $"No value: {Error?.ToString() ?? "result was successful but empty"}");

        public static AiResult<T> Ok(T value) => new(true, value, null);

        public static AiResult<T> Fail(AiError error) => new(false, default, error);

        public static AiResult<T> Fail(AiErrorCode code, string message, int? httpStatus = null,
            bool retryable = false, string? serviceErrorCode = null, TimeSpan? retryAfter = null) =>
            new(false, default, new AiError(code, message, httpStatus, retryable, serviceErrorCode, retryAfter));

        /// <summary>Carry a failure across a type boundary without restating it.</summary>
        public AiResult<TOther> PropagateFailure<TOther>() => IsFailure
            ? AiResult<TOther>.Fail(Error!)
            : throw new InvalidOperationException("Cannot propagate a successful result as a failure.");
    }
}
