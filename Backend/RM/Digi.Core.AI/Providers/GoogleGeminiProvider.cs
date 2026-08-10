using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Digi.Core.AI.Contracts;
using Digi.Core.AI.Providers.Generic;
using Microsoft.Extensions.Logging;

namespace Digi.Core.AI.Providers
{
    /// <summary>
    /// Google's Gemini <c>generateContent</c> API behind the same
    /// <see cref="IAIServiceProvider"/> contract Multinet's service implements —
    /// see <see cref="OpenAiProvider"/>'s remarks for what that does and does not
    /// mean for feature parity.
    /// </summary>
    public sealed class GoogleGeminiProvider : IAIServiceProvider
    {
        // v1 (not v1beta) to match the endpoint shape already validated in this
        // codebase's legacy Google integration; gemini-1.5-* is what v1 serves.
        private static readonly Uri DefaultBaseUri = new("https://generativelanguage.googleapis.com/v1/");

        // The interface has no per-call model parameter yet (see REFACTOR_STATE.md);
        // until it does, this is the one place a model upgrade happens.
        private const string DefaultModel = "gemini-1.5-pro";

        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleGeminiProvider> _logger;

        public GoogleGeminiProvider(HttpClient httpClient, ILogger<GoogleGeminiProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public bool IsStub => false;

        private ChatCompletion Chat(string? apiKey, Uri? baseUriOverride, string? model) => (system, user, ct) =>
            string.IsNullOrWhiteSpace(apiKey)
                ? Task.FromResult(AiResult<string>.Fail(AiErrorCode.RejectedLocally, "No API key configured for Gemini."))
                : CompleteAsync(baseUriOverride ?? DefaultBaseUri, apiKey, system, user, ct, model);

        public async Task<AiResult<KeyVerification>> VerifyKeyAsync(
            string? apiKey = null, Uri? baseUriOverride = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return AiResult<KeyVerification>.Fail(AiErrorCode.RejectedLocally, "No API key configured for Gemini.");
            }

            var baseUri = baseUriOverride ?? DefaultBaseUri;
            try
            {
                var response = await _httpClient.GetAsync(CombineUrl(baseUri, $"models?key={apiKey}"), cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return AiResult<KeyVerification>.Ok(new KeyVerification { Valid = true, Service = "gemini" });
                }

                return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    ? AiResult<KeyVerification>.Fail(AiErrorCode.Unauthorized, "Gemini rejected this API key.", (int)response.StatusCode)
                    : AiResult<KeyVerification>.Fail(AiErrorCode.InternalError, $"Gemini returned HTTP {(int)response.StatusCode}.", (int)response.StatusCode, retryable: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini key verification failed to reach the API.");
                return AiResult<KeyVerification>.Fail(AiErrorCode.Unreachable, $"Could not reach Gemini: {ex.Message}", retryable: true);
            }
        }

        public Task<AiResult<JobRequisitionResult>> GenerateJobRequisitionAsync(
            JobRequisitionRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.GenerateJobRequisitionAsync(request, "Gemini", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<ServiceHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceHealth>.Ok(new ServiceHealth { Status = "ok", Service = "gemini" }));

        public Task<AiResult<ServiceReadiness>> GetReadinessAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceReadiness>.Ok(new ServiceReadiness { Status = "ready", Model = DefaultModel }));

        public Task<AiResult<ServiceVersion>> GetVersionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceVersion>.Ok(new ServiceVersion { Model = DefaultModel }));

        public Task<AiResult<ParseResumeResult>> ExtractResumeAsync(
            Stream content, string fileName, string? apiKey = null, string? model = null, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            return GenericProviderPipeline.ExtractResumeAsync(buffer.ToArray(), fileName, "Gemini", Chat(apiKey, null, model), cancellationToken);
        }

        public Task<AiResult<ParseResumeResult>> ExtractResumeByUrlAsync(
            string documentUrl, string? candidateId = null, string? applicationId = null, string? requisitionId = null,
            string? companyId = null, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.ExtractResumeByUrlAsync(_httpClient, documentUrl, "Gemini", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<ScreenCandidateResult>> ScreenCandidateAsync(
            ScreenCandidateRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.ScreenCandidateAsync(request, "Gemini", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<InterviewQuestionsResult>> GenerateInterviewQuestionsAsync(
            InterviewQuestionsRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.GenerateInterviewQuestionsAsync(request, "Gemini", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<CandidateIndexResult>> ListCandidatesAsync(string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<CandidateIndexResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "Gemini has no candidate corpus of its own — this portal's stored applicants are the only index."));

        public Task<AiResult<RankResult>> RankAsync(string jobDescription, int topK, string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<RankResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "Candidate ranking needs Multinet's embeddings corpus; Gemini has no equivalent here."));

        public Task<AiResult<ScoreResult>> ScoreAsync(string profileId, string jobDescription, string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ScoreResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "Rubric scoring needs Multinet's scoring engine; Gemini has no equivalent here."));

        private async Task<AiResult<string>> CompleteAsync(
            Uri baseUri, string apiKey, string systemPrompt, string userPrompt, CancellationToken cancellationToken, string? model = null)
        {
            // v1 has no dedicated system-instruction field the way v1beta does;
            // folding it into the same turn keeps this on the endpoint shape
            // already proven to work in this codebase.
            var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";

            var requestBody = new
            {
                contents = new object[] { new { parts = new object[] { new { text = combinedPrompt } } } },
                generationConfig = new { maxOutputTokens = 4096, temperature = 0.2 },
            };

            var resolvedModel = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
            var url = CombineUrl(baseUri, $"{resolvedModel}:generateContent?key={apiKey}");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return AiResult<string>.Fail(AiErrorCode.Timeout, "Gemini did not respond in time.", retryable: true);
            }
            catch (HttpRequestException ex)
            {
                return AiResult<string>.Fail(AiErrorCode.Unreachable, $"Could not reach Gemini: {ex.Message}", retryable: true);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return AiResult<string>.Fail(MapError(response.StatusCode, body));
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                return string.IsNullOrWhiteSpace(text)
                    ? AiResult<string>.Fail(AiErrorCode.ContractViolation, "Gemini returned an empty completion.")
                    : AiResult<string>.Ok(text!);
            }
            catch (Exception ex)
            {
                return AiResult<string>.Fail(
                    AiErrorCode.ContractViolation, $"Gemini's response did not match the expected generateContent shape: {ex.Message}");
            }
        }

        private static AiError MapError(HttpStatusCode status, string body)
        {
            string? message = null;
            string? googleStatus = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var err))
                {
                    message = err.TryGetProperty("message", out var m) ? m.GetString() : null;
                    googleStatus = err.TryGetProperty("status", out var s) ? s.GetString() : null;
                }
            }
            catch (JsonException)
            {
                // fall through to a generic message below
            }

            var safeMessage = message ?? $"Gemini returned HTTP {(int)status}.";

            return status switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                    new AiError(AiErrorCode.Unauthorized, "Gemini rejected the API key.", (int)status, false, googleStatus),
                HttpStatusCode.TooManyRequests =>
                    new AiError(AiErrorCode.Busy, safeMessage, (int)status, true, googleStatus),
                _ when (int)status >= 500 =>
                    new AiError(AiErrorCode.InternalError, safeMessage, (int)status, true, googleStatus),
                _ =>
                    new AiError(AiErrorCode.BadRequest, safeMessage, (int)status, false, googleStatus),
            };
        }

        private static Uri CombineUrl(Uri baseUri, string path)
        {
            var basePath = baseUri.ToString();
            if (!basePath.EndsWith('/'))
            {
                basePath += "/";
            }

            return new Uri(basePath + path);
        }
    }
}
