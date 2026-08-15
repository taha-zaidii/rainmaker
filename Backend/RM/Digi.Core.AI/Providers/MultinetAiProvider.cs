using System.Net;
using Microsoft.Extensions.Logging;
using Digi.Core.AI.Contracts;
using Digi.Core.AI.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Digi.Core.AI.Providers
{
    /// <summary>
    /// HTTP implementation of <see cref="IAIServiceProvider"/>.
    ///
    /// Registered as a typed client so HttpClientFactory owns socket lifetime and
    /// the resilience policy (see MultinetAiRegistration). This class holds no
    /// state: the readiness gate and the parse queue are separate concerns, so
    /// this stays a thin, testable translation between the wire contract and the
    /// domain result type.
    /// </summary>
    public sealed class MultinetAiProvider : IAIServiceProvider
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
        private readonly ILogger<MultinetAiProvider> _logger;

        public MultinetAiProvider(
            HttpClient httpClient,
            IOptionsSnapshot<MultinetAiOptions> optionsAccessor,
            ILogger<MultinetAiProvider> logger)
        {
            _http = httpClient;
            _options = optionsAccessor.Value;
            _logger = logger;
        }

        public bool IsStub => false;

        // ── Key verification (the only probe that works through nginx) ───────

        /// <summary>
        /// GET auth/verify. Costs zero GPU and answers in milliseconds, so it is
        /// both the "Test API Key" implementation and the portal's health check.
        ///
        /// A 401 comes back as an <see cref="AiErrorCode.Unauthorized"/> failure
        /// rather than <c>valid: false</c>, which lets callers separate "the key
        /// is wrong" from "we could not reach the service" — conflating those two
        /// is what made the settings page report "API Key Invalid" for a fault
        /// that had nothing to do with the key.
        /// </summary>
        public Task<AiResult<KeyVerification>> VerifyKeyAsync(
            string? apiKey = null,
            Uri? baseUriOverride = null,
            CancellationToken cancellationToken = default) =>
            SendAsync<KeyVerification>(
                () => new HttpRequestMessage(HttpMethod.Get, MultinetAiEndpoints.VerifyKey),
                apiKey,
                cancellationToken,
                baseUriOverride);

        // ── On-box probes ────────────────────────────────────────────────────
        //
        // /health, /ready and /version answer only on the AI service's own host.
        // nginx returns 404 for them at https://ai.rainmaker.pk/hrms/*, so they
        // are useful when running the service locally and useless in production.
        // Nothing in a request path may gate on them — use VerifyKeyAsync.

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
                    return AiResult<ServiceReadiness>.Fail(MapError(response, body));
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

        // ── Job requisition generation ───────────────────────────────────────

        public async Task<AiResult<JobRequisitionResult>> GenerateJobRequisitionAsync(
            JobRequisitionRequest request,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null, // ignored — Multinet pins one resident model server-side
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return AiResult<JobRequisitionResult>.Fail(
                    AiErrorCode.BadRequest, "No job requisition request was supplied.");
            }

            // The service's only hard requirement. Checking here saves a recruiter
            // a 30-second wait for a rejection we can see coming.
            if (string.IsNullOrWhiteSpace(request.JobTitle))
            {
                return AiResult<JobRequisitionResult>.Fail(
                    AiErrorCode.BadRequest,
                    "A job title is required before a job description can be generated.");
            }

            var result = await SendAsync<JobRequisitionResult>(
                () => JsonRequest(MultinetAiEndpoints.GenerateJobRequisition, request),
                apiKey,
                cancellationToken,
                baseUriOverride).ConfigureAwait(false);

            if (result.IsFailure)
            {
                return result;
            }

            var generated = result.Value!;
            if (generated.Data is null)
            {
                return ContractViolation<JobRequisitionResult>(
                    "The AI service reported success but returned no job requisition data.");
            }

            EnforceAdvisoryInvariants(generated);

            _logger.LogInformation(
                "Job requisition generated for '{JobTitle}' in {ElapsedMs} ms (cache {Cache}, " +
                "experience from {ExperienceSource}, category from {CategorySource}).",
                request.JobTitle,
                generated.ExecutionTimeMs ?? 0,
                generated.Meta?.CacheHit == true ? "hit" : "miss",
                generated.Meta?.ExperienceSource ?? "model",
                generated.Meta?.JobCategorySource ?? "model");

            return result;
        }

        /// <summary>
        /// Re-asserts the rules that make AI-assisted hiring lawful, at our own
        /// boundary rather than trusting the service to have held them.
        ///
        /// The service does enforce these, and in normal operation this method
        /// changes nothing. It exists because the failure it guards against is
        /// not "a field looks odd" but "the portal published a discriminatory job
        /// advert", and the cost of a redundant check is three comparisons. A
        /// regression upstream, a proxy rewriting a response, or a future version
        /// relaxing a rule would otherwise reach a recruiter's screen unchallenged.
        ///
        /// Anything corrected here is logged at Error: it means the contract was
        /// violated and the AI team needs the case.
        /// </summary>
        private void EnforceAdvisoryInvariants(JobRequisitionResult generated)
        {
            // Age is a protected attribute. An AI proposing an age band in a job
            // advert is discriminatory and indefensible under the EU AI Act's
            // high-risk hiring rules. It is never displayed, whatever arrives.
            var ageLimits = generated.Data?.Requirements?.AgeLimits;
            if (ageLimits?.HasValue == true)
            {
                _logger.LogError(
                    "The AI service returned age limits ({Min}–{Max}) on a job requisition. " +
                    "This violates the integration contract and has been discarded. " +
                    "Report this to the AI team with the request payload.",
                    ageLimits.Minimum, ageLimits.Maximum);

                generated.Data!.Requirements!.AgeLimits = null;
            }

            var publishing = generated.Data?.Publishing;
            if (publishing is null)
            {
                return;
            }

            // A human publishes. The AI never does.
            if (publishing.IsPublicJob == true)
            {
                _logger.LogError(
                    "The AI service marked a generated requisition as public. Forced back to private.");
                publishing.IsPublicJob = false;
            }

            if (!string.Equals(publishing.Status, "Draft", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "The AI service returned requisition status '{Status}' instead of 'Draft'. Forced to Draft.",
                    publishing.Status ?? "(none)");
                publishing.Status = "Draft";
            }
        }

        // ── The core integration ─────────────────────────────────────────────

        public async Task<AiResult<ParseResumeResult>> ExtractResumeAsync(
            Stream content,
            string fileName,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null, // ignored — Multinet pins one resident model server-side
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
                    // ASCII only — a non-ASCII name gets RFC-2047 encoded and the
                    // service can no longer read the extension. See ToTransportFileName.
                    form.Add(filePart, UploadFieldName, ResumeUploadValidator.ToTransportFileName(fileName));

                    return new HttpRequestMessage(HttpMethod.Post, MultinetAiEndpoints.ExtractResume)
                    {
                        Content = form
                    };
                },
                apiKey,
                cancellationToken,
                baseUriOverride).ConfigureAwait(false);

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

        public async Task<AiResult<ParseResumeResult>> ExtractResumeByUrlAsync(
            string documentUrl,
            string? candidateId = null,
            string? applicationId = null,
            string? requisitionId = null,
            string? companyId = null,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null, // ignored — Multinet pins one resident model server-side
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(documentUrl))
            {
                return AiResult<ParseResumeResult>.Fail(
                    AiErrorCode.BadRequest, "No document URL was supplied.");
            }

            var payload = new
            {
                document_url = documentUrl,
                candidate_id = candidateId,
                application_id = applicationId,
                requisition_id = requisitionId,
                company_id = companyId
            };

            var result = await SendAsync<ParseResumeResult>(
                () => JsonRequest(MultinetAiEndpoints.ExtractResumeByUrl, payload),
                apiKey,
                cancellationToken,
                baseUriOverride).ConfigureAwait(false);

            if (result.IsFailure)
            {
                return result;
            }

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
                "Parse (URL) succeeded via {Route} in {WallMs:0} ms; {FlaggedCount} field(s) flagged for review: {Flagged}",
                parsed.Meta?.ExtractionRoute ?? "unknown",
                parsed.Meta?.TotalWallMs ?? 0,
                flagged.Count,
                flagged.Count == 0 ? "none" : string.Join(", ", flagged));

            return result;
        }

        public async Task<AiResult<ScreenCandidateResult>> ScreenCandidateAsync(
            ScreenCandidateRequest request,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null, // ignored — Multinet pins one resident model server-side
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return AiResult<ScreenCandidateResult>.Fail(
                    AiErrorCode.BadRequest, "Screening request cannot be null.");
            }

            var result = await SendAsync<ScreenCandidateResult>(
                () => JsonRequest(MultinetAiEndpoints.ScreenCandidate, request),
                apiKey,
                cancellationToken,
                baseUriOverride).ConfigureAwait(false);

            if (result.IsFailure)
            {
                return result;
            }

            var screened = result.Value!;
            screened.ReviewRequired = true;
            screened.Advisory = true;

            _logger.LogInformation(
                "Resume screening completed for '{JobTitle}' with score {Score} (Shortlisted: {Shortlisted}, Threshold: {ThresholdUsed}).",
                request.JobTitle,
                screened.MatchScore,
                screened.Shortlisted,
                screened.ThresholdUsed);

            return result;
        }

        public async Task<AiResult<InterviewQuestionsResult>> GenerateInterviewQuestionsAsync(
            InterviewQuestionsRequest request,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null, // ignored — Multinet pins one resident model server-side
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return AiResult<InterviewQuestionsResult>.Fail(
                    AiErrorCode.BadRequest, "Interview questions request cannot be null.");
            }

            var result = await SendAsync<InterviewQuestionsResult>(
                () => JsonRequest(MultinetAiEndpoints.InterviewQuestions, request),
                apiKey,
                cancellationToken,
                baseUriOverride).ConfigureAwait(false);

            if (result.IsFailure)
            {
                return result;
            }

            var generated = result.Value!;
            generated.ReviewRequired = true;
            generated.Advisory = true;

            _logger.LogInformation(
                "Generated interview questions for '{JobTitle}' across {CategoryCount} categories.",
                request.JobTitle,
                generated.QuestionBank?.Count ?? 0);

            return result;
        }


        // ── Matching and scoring ─────────────────────────────────────────────


        public Task<AiResult<CandidateIndexResult>> ListCandidatesAsync(
            string? apiKey = null,
            CancellationToken cancellationToken = default) =>
            SendAsync<CandidateIndexResult>(
                () => new HttpRequestMessage(HttpMethod.Get, "candidates"), apiKey, cancellationToken);

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
                () => JsonRequest(MultinetAiEndpoints.RankCandidates, new { jd_text = jd, top_k = topK }),
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
                () => JsonRequest(MultinetAiEndpoints.ScoreCandidate, new { profile_id = id, jd_text = jd }),
                apiKey,
                cancellationToken);
        }

        // ── Plumbing ─────────────────────────────────────────────────────────

        /// <summary>
        /// Outgoing payloads OMIT nulls rather than sending them. The contract is
        /// deliberately tolerant of incomplete ERP data and asks callers to leave
        /// a field out when it has no value; an explicit null is a weaker but
        /// still different statement, and omission is what it documents.
        /// </summary>
        private static readonly JsonSerializerOptions RequestJson = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static HttpRequestMessage JsonRequest(string path, object payload) =>
            new(HttpMethod.Post, path)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload, RequestJson), Encoding.UTF8, "application/json")
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
            CancellationToken cancellationToken,
            Uri? baseUriOverride = null)
        {
            var effectiveKey = string.IsNullOrWhiteSpace(apiKey) ? _options.ApiKey : apiKey;

            try
            {
                using var request = requestFactory();

                // The portal is multi-tenant: each company stores its own AI
                // endpoint, so the client's configured BaseAddress is only a
                // default. An absolute RequestUri overrides it for this call
                // while keeping the pooled handler, the retry policy and the
                // timeout — all of which we still want.
                if (baseUriOverride is not null &&
                    request.RequestUri is not null &&
                    !request.RequestUri.IsAbsoluteUri)
                {
                    request.RequestUri = MultinetAiEndpoints.Combine(
                        baseUriOverride, request.RequestUri.ToString());
                }

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
                    var error = MapError(response, body);
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
        /// Maps an HTTP failure onto the domain, reading the <c>Retry-After</c>
        /// header as well as the body. Prefer this overload wherever a response
        /// is in hand — the header is the service's own instruction and beats
        /// any schedule we would invent.
        /// </summary>
        internal static AiError MapError(HttpResponseMessage response, string? body) =>
            MapError(response.StatusCode, body, ReadRetryAfterHeader(response));

        /// <summary>
        /// Maps an HTTP failure onto the domain. Retryability is the important
        /// output: a 422 is a verdict about the document and must never be
        /// retried, while a 5xx or 408 is worth another attempt.
        /// </summary>
        internal static AiError MapError(HttpStatusCode status, string? body) =>
            MapError(status, body, null);

        internal static AiError MapError(HttpStatusCode status, string? body, TimeSpan? retryAfterHeader)
        {
            var (serviceCode, serviceMessage, retryAfterSeconds) = ReadErrorPayload(body);

            // Header first, body second. Both are the service telling us how long
            // to wait; the header is the HTTP-standard form, the body field is
            // what this service actually sends on a 429.
            var retryAfter = retryAfterHeader
                             ?? (retryAfterSeconds is > 0
                                 ? TimeSpan.FromSeconds(retryAfterSeconds.Value)
                                 : (TimeSpan?)null);

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

                // A 404 from the edge means the base URL is wrong far more often
                // than it means anything else, and nginx's own body is a bare
                // "Not Found" that tells an administrator nothing. Our message
                // wins here; the service code is still kept for the logs.
                HttpStatusCode.NotFound => new AiError(
                    serviceCode == "unknown_profile_id" ? AiErrorCode.UnknownProfileId : AiErrorCode.BadRequest,
                    serviceCode == "unknown_profile_id"
                        ? "The AI service has no parsed profile with that id."
                        : "No AI endpoint exists at that address. The API Endpoint should be the versioned " +
                          "base URL, for example https://ai.rainmaker.pk/hrms/api/v1 — the backend appends " +
                          "the rest of the path itself.",
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

                // Not a fault: the GPU processes work serially, so a 429 is the
                // service queueing us politely and saying for how long.
                HttpStatusCode.TooManyRequests => new AiError(
                    AiErrorCode.Busy,
                    retryAfter is null
                        ? "The AI service is busy with another request. The work will be retried."
                        : "The AI service is busy with another request. Retrying in about " +
                          $"{Math.Ceiling(retryAfter.Value.TotalSeconds):0} seconds.",
                    (int)status, true, serviceCode ?? "busy", retryAfter),

                HttpStatusCode.ServiceUnavailable => new AiError(
                    AiErrorCode.NotReady,
                    serviceCode == "llm_unreachable"
                        ? "The AI service's model backend is not responding. The work will be retried."
                        : "The AI service is not ready to accept work.",
                    (int)status, true, serviceCode ?? "not_ready", retryAfter),

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
        /// Convenience wrapper kept for callers that do not care about the
        /// retry hint. See <see cref="ReadErrorPayload"/> for the shapes handled.
        /// </summary>
        internal static (string? Code, string? Message) ReadErrorDetail(string? body)
        {
            var (code, message, _) = ReadErrorPayload(body);
            return (code, message);
        }

        /// <summary>
        /// Reads an error body. The service uses TWO shapes and both have to be
        /// understood, which is easy to get wrong because only one is obvious:
        ///
        ///   401 / 422  →  {"detail": {"error": "<slug>", "message": "..."}}
        ///   400        →  {"detail": "No filename provided."}          (bare string)
        ///   429        →  {"error": "busy", "retry_after_s": 12}       (root level)
        ///   503 / 500  →  {"error": "llm_unreachable"}                 (root level)
        ///
        /// Reading only <c>detail</c> silently discards the 429 wait hint and the
        /// 503 slug — which is exactly the information needed to behave well
        /// against a single-flight GPU.
        ///
        /// Anything unparseable yields nulls rather than throwing: a mangled body
        /// (an nginx HTML error page, say) must never mask the status code.
        /// </summary>
        internal static (string? Code, string? Message, int? RetryAfterSeconds) ReadErrorPayload(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return (null, null, null);
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    return (null, null, null);
                }

                int? retryAfterSeconds = null;
                if (root.TryGetProperty("retry_after_s", out var retry))
                {
                    if (retry.ValueKind == JsonValueKind.Number && retry.TryGetInt32(out var seconds))
                    {
                        retryAfterSeconds = seconds;
                    }
                    else if (retry.ValueKind == JsonValueKind.String &&
                             int.TryParse(retry.GetString(), out var parsedSeconds))
                    {
                        retryAfterSeconds = parsedSeconds;
                    }
                }

                if (root.TryGetProperty("detail", out var detail))
                {
                    switch (detail.ValueKind)
                    {
                        case JsonValueKind.String:
                            return (null, detail.GetString(), retryAfterSeconds);

                        case JsonValueKind.Object:
                            return (
                                ReadString(detail, "error"),
                                ReadString(detail, "message"),
                                retryAfterSeconds);
                    }
                }

                return (ReadString(root, "error"), ReadString(root, "message"), retryAfterSeconds);
            }
            catch (JsonException)
            {
                return (null, null, null);
            }
        }

        private static string? ReadString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        /// <summary>
        /// Reads the standard <c>Retry-After</c> header in either of its forms —
        /// delta-seconds or an HTTP date. A date already in the past yields zero
        /// rather than a negative delay.
        /// </summary>
        private static TimeSpan? ReadRetryAfterHeader(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter;

            if (retryAfter is null)
            {
                return null;
            }

            if (retryAfter.Delta is { } delta)
            {
                return delta;
            }

            if (retryAfter.Date is { } date)
            {
                var wait = date - DateTimeOffset.UtcNow;
                return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
            }

            return null;
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
