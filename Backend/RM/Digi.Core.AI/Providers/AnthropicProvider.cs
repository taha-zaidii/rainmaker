using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Digi.Core.AI.Contracts;
using Digi.Core.AI.Providers.Generic;
using Microsoft.Extensions.Logging;

namespace Digi.Core.AI.Providers
{
    /// <summary>
    /// Anthropic's Messages API behind the same <see cref="IAIServiceProvider"/>
    /// contract Multinet's service implements — see <see cref="OpenAiProvider"/>'s
    /// remarks for what that does and does not mean for feature parity.
    /// </summary>
    public sealed class AnthropicProvider : IAIServiceProvider
    {
        private static readonly Uri DefaultBaseUri = new("https://api.anthropic.com/v1/");
        private const string AnthropicVersion = "2023-06-01";

        // The interface has no per-call model parameter yet (see REFACTOR_STATE.md);
        // until it does, this is the one place a model upgrade happens.
        private const string DefaultModel = "claude-3-5-sonnet-20241022";

        private readonly HttpClient _httpClient;
        private readonly ILogger<AnthropicProvider> _logger;

        public AnthropicProvider(HttpClient httpClient, ILogger<AnthropicProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public bool IsStub => false;

        private ChatCompletion Chat(string? apiKey, Uri? baseUriOverride, string? model) => (system, user, ct) =>
            string.IsNullOrWhiteSpace(apiKey)
                ? Task.FromResult(AiResult<string>.Fail(AiErrorCode.RejectedLocally, "No API key configured for Anthropic."))
                : CompleteAsync(baseUriOverride ?? DefaultBaseUri, apiKey, system, user, ct, model: model);

        public async Task<AiResult<KeyVerification>> VerifyKeyAsync(
            string? apiKey = null, Uri? baseUriOverride = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return AiResult<KeyVerification>.Fail(AiErrorCode.RejectedLocally, "No API key configured for Anthropic.");
            }

            // Anthropic has no standalone "verify key" endpoint, so the cheapest
            // real message (1 output token) is the honest way to know a key works
            // — the same reasoning the existing hand-rolled test used.
            var probe = await CompleteAsync(baseUriOverride ?? DefaultBaseUri, apiKey, "Reply with only the word OK.", "OK?", cancellationToken, maxTokens: 5);

            if (probe.IsSuccess)
            {
                return AiResult<KeyVerification>.Ok(new KeyVerification { Valid = true, Service = "anthropic" });
            }

            _logger.LogWarning("Anthropic key verification failed: {Error}", probe.Error);
            return probe.PropagateFailure<KeyVerification>();
        }

        public Task<AiResult<JobRequisitionResult>> GenerateJobRequisitionAsync(
            JobRequisitionRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.GenerateJobRequisitionAsync(request, "Anthropic", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<ServiceHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceHealth>.Ok(new ServiceHealth { Status = "ok", Service = "anthropic" }));

        public Task<AiResult<ServiceReadiness>> GetReadinessAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceReadiness>.Ok(new ServiceReadiness { Status = "ready", Model = DefaultModel }));

        public Task<AiResult<ServiceVersion>> GetVersionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceVersion>.Ok(new ServiceVersion { Model = DefaultModel }));

        public Task<AiResult<ParseResumeResult>> ExtractResumeAsync(
            Stream content, string fileName, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            return GenericProviderPipeline.ExtractResumeAsync(buffer.ToArray(), fileName, "Anthropic", Chat(apiKey, baseUriOverride, model), cancellationToken);
        }

        public Task<AiResult<ParseResumeResult>> ExtractResumeByUrlAsync(
            string documentUrl, string? candidateId = null, string? applicationId = null, string? requisitionId = null,
            string? companyId = null, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.ExtractResumeByUrlAsync(_httpClient, documentUrl, "Anthropic", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<ScreenCandidateResult>> ScreenCandidateAsync(
            ScreenCandidateRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.ScreenCandidateAsync(request, "Anthropic", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<InterviewQuestionsResult>> GenerateInterviewQuestionsAsync(
            InterviewQuestionsRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.GenerateInterviewQuestionsAsync(request, "Anthropic", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<CandidateIndexResult>> ListCandidatesAsync(string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<CandidateIndexResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "Anthropic has no candidate corpus of its own — this portal's stored applicants are the only index."));

        public Task<AiResult<RankResult>> RankAsync(string jobDescription, int topK, string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<RankResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "Candidate ranking needs Multinet's embeddings corpus; Anthropic has no equivalent here."));

        public Task<AiResult<ScoreResult>> ScoreAsync(string profileId, string jobDescription, string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ScoreResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "Rubric scoring needs Multinet's scoring engine; Anthropic has no equivalent here."));

        private async Task<AiResult<string>> CompleteAsync(
            Uri baseUri, string apiKey, string systemPrompt, string userPrompt, CancellationToken cancellationToken,
            int maxTokens = 4096, string? model = null)
        {
            var requestBody = new
            {
                model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model,
                max_tokens = maxTokens,
                temperature = 0.2,
                system = systemPrompt,
                messages = new object[] { new { role = "user", content = userPrompt } },
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, CombineUrl(baseUri, "messages"))
            {
                Content = JsonContent.Create(requestBody),
            };
            httpRequest.Headers.Add("x-api-key", apiKey);
            httpRequest.Headers.Add("anthropic-version", AnthropicVersion);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return AiResult<string>.Fail(AiErrorCode.Timeout, "Anthropic did not respond in time.", retryable: true);
            }
            catch (HttpRequestException ex)
            {
                return AiResult<string>.Fail(AiErrorCode.Unreachable, $"Could not reach Anthropic: {ex.Message}", retryable: true);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return AiResult<string>.Fail(MapError(response.StatusCode, body));
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
                return string.IsNullOrWhiteSpace(text)
                    ? AiResult<string>.Fail(AiErrorCode.ContractViolation, "Anthropic returned an empty completion.")
                    : AiResult<string>.Ok(text!);
            }
            catch (Exception ex)
            {
                return AiResult<string>.Fail(
                    AiErrorCode.ContractViolation, $"Anthropic's response did not match the expected messages shape: {ex.Message}");
            }
        }

        private static AiError MapError(HttpStatusCode status, string body)
        {
            string? message = null;
            string? type = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var err))
                {
                    type = err.TryGetProperty("type", out var t) ? t.GetString() : null;
                    message = err.TryGetProperty("message", out var m) ? m.GetString() : null;
                }
            }
            catch (JsonException)
            {
                // fall through to a generic message below
            }

            var safeMessage = message ?? $"Anthropic returned HTTP {(int)status}.";

            return status switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                    new AiError(AiErrorCode.Unauthorized, "Anthropic rejected the API key.", (int)status, false, type),
                HttpStatusCode.TooManyRequests =>
                    new AiError(AiErrorCode.Busy, safeMessage, (int)status, true, type),
                _ when (int)status >= 500 =>
                    new AiError(AiErrorCode.InternalError, safeMessage, (int)status, true, type),
                _ =>
                    new AiError(AiErrorCode.BadRequest, safeMessage, (int)status, false, type),
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
