using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Digi.Recruitment.Module.Domain.AI.Multinet
{
    /// <summary>
    /// HTTP implementation of <see cref="IMultinetAiClient"/>.
    ///
    /// Registered as a typed client so HttpClientFactory owns socket lifetime and
    /// the resilience policy (see MultinetAiRegistration). This class holds no
    /// state: the readiness gate and the parse queue are separate concerns, so
    /// this stays a thin, testable translation between the wire contract and the
    /// domain result type.
    /// </summary>
    public sealed class MultinetAiClient : IMultinetAiClient
    {
        /// <summary>Header the service authenticates on. It is fail-closed: no key, no business endpoint.</summary>
        internal const string ApiKeyHeader = "X-API-Key";

        /// <summary>Form field name for the upload. Frozen by the contract.</summary>
        internal const string UploadFieldName = "file";

        internal static readonly JsonSerializerOptions Json = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private readonly HttpClient _http;
        private readonly MultinetAiOptions _options;
        private readonly ILogger<MultinetAiClient> _logger;

        public MultinetAiClient(
            HttpClient http,
            IOptions<MultinetAiOptions> options,
            ILogger<MultinetAiClient> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public bool IsStub => false;

        // ── Probes (open endpoints, no key) ──────────────────────────────────

        public Task<AiResult<ServiceHealth>> GetHealthAsync(CancellationToken cancellationToken = default) =>
            SendAsync<ServiceHealth>(() => new HttpRequestMessage(HttpMethod.Get, "health"), null, cancellationToken);

        public async Task<AiResult<ServiceReadiness>> GetReadinessAsync(CancellationToken cancellationToken = default)
        {
            // /ready answers 503 with a MEANINGFUL body when the GPU backend is
            // down. That is not an error to swallow — the queue needs to tell the
            // difference between "park this job" and "something is broken".
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "ready");
                using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    var notReady = Deserialize<ServiceReadiness>(body);
                    var backend = notReady?.LlmBackend?.Reachable == false ? "LLM backend unreachable" : "not ready";
                    return AiResult<ServiceReadiness>.Fail(
                        AiErrorCode.NotReady,
                        $"The AI service is not ready to accept work ({backend}).",
                        (int)response.StatusCode,
                        retryable: true,
                        serviceErrorCode: "not_ready");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return AiResult<ServiceReadiness>.Fail(MapError(response.StatusCode, body));
                }

                var readiness = Deserialize<ServiceReadiness>(body);
                return readiness is null
                    ? ContractViolation<ServiceReadiness>("GET /ready returned a body that could not be read.")
                    : AiResult<ServiceReadiness>.Ok(readiness);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelled by us (shutdown, client disconnect) — not a service fault.
                throw;
            }
            catch (Exception ex)
            {
                return MapTransport<ServiceReadiness>(ex, "GET /ready");
            }
        }

        public Task<AiResult<ServiceVersion>> GetVersionAsync(CancellationToken cancellationToken = default) =>
            SendAsync<ServiceVersion>(() => new HttpRequestMessage(HttpMethod.Get, "version"), null, cancellationToken);

        // ── The core integration ─────────────────────────────────────────────

        public async Task<AiResult<ParseResumeResult>> ExtractResumeAsync(
            Stream content,
            string fileName,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            if (content is null)
            {
                return AiResult<ParseResumeResult>.Fail(
                    AiErrorCode.BadRequest, "No file content was supplied.");
            }

            // Reject locally what the service would reject anyway, so a recruiter
            // is not made to wait behind a GPU queue for a predictable "no".
            var header = await PeekHeaderAsync(content, cancellationToken).ConfigureAwait(false);
            var sizeBytes = content.CanSeek ? content.Length : -1;
            var localError = ResumeUploadValidator.Validate(
                fileName,
                sizeBytes < 0 ? 1 : sizeBytes,      // unknown length: let the service decide on size
                header.Span,
                _options);

            if (localError is not null)
            {
                _logger.LogInformation(
                    "Resume upload rejected before dispatch: {Code} ({File})", localError.Code, fileName);
                return AiResult<ParseResumeResult>.Fail(localError);
            }

            var extension = Path.GetExtension(fileName);

            var result = await SendAsync<ParseResumeResult>(
                () =>
                {
                    // Rebuilt per attempt: a retry cannot reuse a consumed stream
                    // or a disposed MultipartFormDataContent.
                    if (content.CanSeek)
                    {
                        content.Position = 0;
                    }

                    var form = new MultipartFormDataContent();
                    var filePart = new StreamContent(content);
                    filePart.Headers.ContentType =
                        new MediaTypeHeaderValue(ResumeUploadValidator.ContentTypeFor(extension));
                    form.Add(filePart, UploadFieldName, Path.GetFileName(fileName));

                    return new HttpRequestMessage(HttpMethod.Post, "api/v1/parser/extract")
                    {
                        Content = form
                    };
                },
                apiKey,
                cancellationToken).ConfigureAwait(false);

            if (result.IsFailure)
            {
                return result;
            }

            // A 200 that does not match the frozen contract is worse than an
            // error: it would write a half-empty candidate record. Refuse it.
            var parsed = result.Value!;
            if (parsed.Data is null)
            {
                return ContractViolation<ParseResumeResult>(
                    "The AI service returned success but no profile data.");
            }

            var schemaVersion = parsed.Meta?.SchemaVersion;
            if (!ProfileSchemaVersions.IsCompatible(schemaVersion))
            {
                _logger.LogError(
                    "ProfileSchema version mismatch: service reported '{Actual}', this build supports '{Supported}'.",
                    schemaVersion ?? "(none)", ProfileSchemaVersions.Supported);

                return ContractViolation<ParseResumeResult>(
                    $"The AI service returned ProfileSchema '{schemaVersion ?? "unknown"}' but this build " +
                    $"supports '{ProfileSchemaVersions.Supported}'. Refusing to store a profile that may be " +
                    "misinterpreted — the integration needs updating.");
            }

            var flagged = parsed.Meta?.FieldsNeedingReview() ?? Array.Empty<string>();
            _logger.LogInformation(
                "Parse succeeded via {Route} in {WallMs:0} ms; {FlaggedCount} field(s) flagged for review: {Flagged}",
                parsed.Meta?.ExtractionRoute ?? "unknown",
                parsed.Meta?.TotalWallMs ?? 0,
                flagged.Count,
                flagged.Count == 0 ? "none" : string.Join(", ", flagged));

            return result;
        }

        // ── Matching and scoring ─────────────────────────────────────────────

        public Task<AiResult<CandidateIndexResult>> ListCandidatesAsync(
            string? apiKey = null,
            CancellationToken cancellationToken = default) =>
            SendAsync<CandidateIndexResult>(
                () => new HttpRequestMessage(HttpMethod.Get, "api/v1/candidates"), apiKey, cancellationToken);

        public Task<AiResult<RankResult>> RankAsync(
            string jobDescription,
            int topK,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            // The service enforces these; failing here saves a round trip and
            // gives a message a recruiter can act on.
            var jd = jobDescription?.Trim() ?? string.Empty;
            if (jd.Length < 30)
            {
                return Task.FromResult(AiResult<RankResult>.Fail(
                    AiErrorCode.BadRequest,
                    "The job description must be at least 30 characters for ranking to be meaningful."));
            }

            if (topK is < 1 or > 50)
            {
                return Task.FromResult(AiResult<RankResult>.Fail(
                    AiErrorCode.BadRequest, "topK must be between 1 and 50."));
            }

            return SendAsync<RankResult>(
                () => JsonRequest("api/v1/matching/rank", new { jd_text = jd, top_k = topK }),
                apiKey,
                cancellationToken);
        }

        public Task<AiResult<ScoreResult>> ScoreAsync(
            string profileId,
            string jobDescription,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            var id = profileId?.Trim() ?? string.Empty;
            var jd = jobDescription?.Trim() ?? string.Empty;

            if (id.Length == 0)
            {
                return Task.FromResult(AiResult<ScoreResult>.Fail(
                    AiErrorCode.BadRequest, "A profile id is required."));
            }

            if (jd.Length < 30)
            {
                return Task.FromResult(AiResult<ScoreResult>.Fail(
                    AiErrorCode.BadRequest,
                    "The job description must be at least 30 characters for scoring to be meaningful."));
            }

            return SendAsync<ScoreResult>(
                () => JsonRequest("api/v1/scoring/score", new { profile_id = id, jd_text = jd }),
                apiKey,
                cancellationToken);
        }

        // ── Plumbing ─────────────────────────────────────────────────────────

        private static HttpRequestMessage JsonRequest(string path, object payload) =>
            new(HttpMethod.Post, path)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

        /// <summary>
        /// Issues the request, attaches auth, and turns the response into a
        /// result. <paramref name="requestFactory"/> is a factory rather than a
        /// message because the resilience policy may replay the call, and an
        /// HttpRequestMessage cannot be sent twice.
        /// </summary>
        private async Task<AiResult<T>> SendAsync<T>(
            Func<HttpRequestMessage> requestFactory,
            string? apiKey,
            CancellationToken cancellationToken)
        {
            var effectiveKey = string.IsNullOrWhiteSpace(apiKey) ? _options.ApiKey : apiKey;

            try
            {
                using var request = requestFactory();

                if (!string.IsNullOrWhiteSpace(effectiveKey))
                {
                    // Never logged, never echoed in an error message.
                    request.Headers.TryAddWithoutValidation(ApiKeyHeader, effectiveKey);
                }

                using var response = await _http
                    .SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                    .ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = MapError(response.StatusCode, body);
                    _logger.LogWarning(
                        "AI service {Method} {Path} → {Status} {Code}: {Message}",
                        request.Method, request.RequestUri, (int)response.StatusCode, error.Code, error.Message);
                    return AiResult<T>.Fail(error);
                }

                var value = Deserialize<T>(body);
                return value is null
                    ? ContractViolation<T>($"The AI service returned a body that could not be read as {typeof(T).Name}.")
                    : AiResult<T>.Ok(value);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelled by us, not by the timeout — let it propagate untouched.
                throw;
            }
            catch (Exception ex)
            {
                return MapTransport<T>(ex, "call the AI service");
            }
        }

        private static T? Deserialize<T>(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(body, Json);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        private AiResult<T> ContractViolation<T>(string message)
        {
            _logger.LogError("AI contract violation: {Message}", message);
            return AiResult<T>.Fail(AiErrorCode.ContractViolation, message);
        }

        /// <summary>
        /// Transport-level failure: nothing came back, so there is no status to
        /// classify. Caller-requested cancellation is filtered out before this is
        /// reached, so every case here is a genuine service problem.
        /// </summary>
        private AiResult<T> MapTransport<T>(Exception ex, string what)
        {
            switch (ex)
            {
                // HttpClient surfaces its own timeout as a TaskCanceledException
                // whose inner exception is a TimeoutException (.NET 5+).
                case OperationCanceledException:
                case TimeoutException:
                    _logger.LogWarning(ex, "Timed out trying to {What} after {Timeout}s.", what, _options.TimeoutSeconds);
                    return AiResult<T>.Fail(
                        AiErrorCode.Timeout,
                        $"The AI service did not respond within {_options.TimeoutSeconds} seconds. " +
                        "It processes one resume at a time, so it may be busy with another document.",
                        retryable: true);

                case HttpRequestException hre:
                    _logger.LogWarning(ex, "Could not {What}: {Reason}", what, hre.Message);
                    return AiResult<T>.Fail(
                        AiErrorCode.Unreachable,
                        "The AI service could not be reached. It may not be running, " +
                        "or the configured address may be wrong.",
                        retryable: true);

                default:
                    _logger.LogError(ex, "Unexpected failure trying to {What}.", what);
                    return AiResult<T>.Fail(
                        AiErrorCode.InternalError,
                        "An unexpected error occurred talking to the AI service.",
                        retryable: false);
            }
        }

        /// <summary>
        /// Maps an HTTP failure onto the domain. Retryability is the important
        /// output: a 422 is a verdict about the document and must never be
        /// retried, while a 5xx or 408 is worth another attempt.
        /// </summary>
        internal static AiError MapError(HttpStatusCode status, string? body)
        {
            var (serviceCode, serviceMessage) = ReadErrorDetail(body);

            return status switch
            {
                HttpStatusCode.BadRequest => new AiError(
                    AiErrorCode.BadRequest,
                    serviceMessage ?? "The AI service rejected the request as malformed.",
                    (int)status, false, serviceCode),

                HttpStatusCode.Unauthorized => new AiError(
                    AiErrorCode.Unauthorized,
                    "The AI service rejected our API key. It is missing, wrong, or has been revoked.",
                    (int)status, false, serviceCode ?? "unauthorized"),

                HttpStatusCode.Forbidden => new AiError(
                    AiErrorCode.Unauthorized,
                    "The API key is not permitted to use this endpoint.",
                    (int)status, false, serviceCode),

                HttpStatusCode.NotFound => new AiError(
                    serviceCode == "unknown_profile_id" ? AiErrorCode.UnknownProfileId : AiErrorCode.BadRequest,
                    serviceCode == "unknown_profile_id"
                        ? "The AI service has no parsed profile with that id."
                        : serviceMessage ?? "The requested AI endpoint does not exist.",
                    (int)status, false, serviceCode),

                HttpStatusCode.RequestEntityTooLarge => new AiError(
                    AiErrorCode.FileTooLarge,
                    serviceMessage ?? "The file is larger than the AI service accepts.",
                    (int)status, false, serviceCode ?? "file_too_large"),

                HttpStatusCode.UnprocessableEntity => serviceCode switch
                {
                    "unsupported_file_type" => new AiError(
                        AiErrorCode.UnsupportedFileType,
                        serviceMessage ?? "That file type is not supported.",
                        (int)status, false, serviceCode),

                    "content_type_mismatch" => new AiError(
                        AiErrorCode.ContentTypeMismatch,
                        serviceMessage ?? "The file contents do not match its extension.",
                        (int)status, false, serviceCode),

                    "extraction_failed" => new AiError(
                        AiErrorCode.ExtractionFailed,
                        serviceMessage ?? "The document could not be turned into a profile. " +
                        "It may be an image-only scan of very low quality, or not a resume.",
                        (int)status, false, serviceCode),

                    "file_processing_error" => new AiError(
                        AiErrorCode.FileProcessingError,
                        serviceMessage ?? "The uploaded file could not be processed. It may be corrupt.",
                        (int)status, false, serviceCode),

                    _ => new AiError(
                        AiErrorCode.ExtractionFailed,
                        serviceMessage ?? "The AI service could not process the document.",
                        (int)status, false, serviceCode)
                },

                HttpStatusCode.RequestTimeout => new AiError(
                    AiErrorCode.Timeout, "The AI service timed out.", (int)status, true, serviceCode),

                HttpStatusCode.TooManyRequests => new AiError(
                    AiErrorCode.NotReady,
                    "The AI service is rate limiting us. The work will be retried.",
                    (int)status, true, serviceCode),

                HttpStatusCode.ServiceUnavailable => new AiError(
                    AiErrorCode.NotReady,
                    "The AI service is not ready to accept work.",
                    (int)status, true, serviceCode ?? "not_ready"),

                _ when (int)status >= 500 => new AiError(
                    AiErrorCode.InternalError,
                    "The AI service reported an internal error.",
                    (int)status, true, serviceCode ?? "internal_error"),

                _ => new AiError(
                    AiErrorCode.InternalError,
                    serviceMessage ?? $"The AI service returned an unexpected status ({(int)status}).",
                    (int)status, false, serviceCode)
            };
        }

        /// <summary>
        /// Reads <c>{"detail": {"error": ..., "message": ...}}</c>. One path (400,
        /// missing filename) returns <c>detail</c> as a bare string, so both
        /// shapes are handled; anything else yields nulls rather than throwing,
        /// because an unparseable error body must not mask the status code.
        /// </summary>
        internal static (string? Code, string? Message) ReadErrorDetail(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return (null, null);
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                if (!document.RootElement.TryGetProperty("detail", out var detail))
                {
                    return (null, null);
                }

                switch (detail.ValueKind)
                {
                    case JsonValueKind.String:
                        return (null, detail.GetString());

                    case JsonValueKind.Object:
                        var code = detail.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String
                            ? e.GetString()
                            : null;
                        var message = detail.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                            ? m.GetString()
                            : null;
                        return (code, message);

                    default:
                        return (null, null);
                }
            }
            catch (JsonException)
            {
                return (null, null);
            }
        }

        /// <summary>
        /// Reads the leading bytes for the magic-byte check and rewinds. Returns
        /// empty for a non-seekable stream, which skips the local content check
        /// and defers to the service rather than consuming the upload.
        /// </summary>
        private static async Task<ReadOnlyMemory<byte>> PeekHeaderAsync(Stream content, CancellationToken ct)
        {
            if (!content.CanSeek || !content.CanRead)
            {
                return ReadOnlyMemory<byte>.Empty;
            }

            var origin = content.Position;
            var buffer = new byte[ResumeUploadValidator.MagicByteWindow];
            var read = await content.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, ct)
                .ConfigureAwait(false);
            content.Position = origin;

            return new ReadOnlyMemory<byte>(buffer, 0, read);
        }
    }
}
