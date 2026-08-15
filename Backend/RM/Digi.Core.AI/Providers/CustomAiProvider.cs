using System.Net.Http.Headers;
using Digi.Core.AI.Contracts;
using Digi.Core.AI.Providers.Generic;
using Microsoft.Extensions.Logging;

namespace Digi.Core.AI.Providers
{
    /// <summary>
    /// "Custom" is the portal's escape hatch for a third-party service a client
    /// brings themselves — Groq, DeepSeek, a self-hosted vLLM/Ollama gateway —
    /// almost all of which speak OpenAI's <c>/chat/completions</c> shape. This is
    /// therefore the same wire protocol as <see cref="OpenAiProvider"/>
    /// (<see cref="Generic.OpenAiCompatibleChat"/>), the one difference being
    /// that there is no sensible default base URL: the client's own endpoint is
    /// mandatory, never assumed. See CLAUDE.md §6 — this is the "custom has no
    /// backend at all today" gap being closed.
    /// </summary>
    public sealed class CustomAiProvider : IAIServiceProvider
    {
        // Most OpenAI-compatible gateways accept any model string (or route by
        // API key instead), so this is a reasonable placeholder — swap for the
        // company's configured Model once the interface carries one per call.
        private const string DefaultModel = "gpt-3.5-turbo";

        private readonly HttpClient _httpClient;
        private readonly ILogger<CustomAiProvider> _logger;

        public CustomAiProvider(HttpClient httpClient, ILogger<CustomAiProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public bool IsStub => false;

        private ChatCompletion Chat(string? apiKey, Uri? baseUriOverride, string? model) => (system, user, ct) =>
            baseUriOverride is null
                ? Task.FromResult(AiResult<string>.Fail(
                    AiErrorCode.RejectedLocally, "A custom provider needs its own API endpoint configured — none was supplied."))
                : string.IsNullOrWhiteSpace(apiKey)
                    ? Task.FromResult(AiResult<string>.Fail(AiErrorCode.RejectedLocally, "No API key configured for the custom endpoint."))
                    : OpenAiCompatibleChat.CompleteAsync(
                        _httpClient, baseUriOverride, apiKey,
                        string.IsNullOrWhiteSpace(model) ? DefaultModel : model, system, user, "the custom AI endpoint", ct);

        public async Task<AiResult<KeyVerification>> VerifyKeyAsync(
            string? apiKey = null, Uri? baseUriOverride = null, CancellationToken cancellationToken = default)
        {
            if (baseUriOverride is null)
            {
                return AiResult<KeyVerification>.Fail(
                    AiErrorCode.RejectedLocally, "A custom provider needs its own API endpoint configured — none was supplied.");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return AiResult<KeyVerification>.Fail(AiErrorCode.RejectedLocally, "No API key configured for the custom endpoint.");
            }

            // There is no universal health/models probe across arbitrary
            // OpenAI-compatible gateways, so a minimal real completion is the
            // most honest check available — the same reasoning Multinet's
            // client uses for preferring a cheap real call over a guess.
            var probe = await OpenAiCompatibleChat.CompleteAsync(
                _httpClient, baseUriOverride, apiKey, DefaultModel,
                "Reply with only the word OK.", "OK?", "the custom AI endpoint", cancellationToken);

            if (probe.IsSuccess)
            {
                return AiResult<KeyVerification>.Ok(new KeyVerification { Valid = true, Service = "custom" });
            }

            _logger.LogWarning("Custom AI endpoint key verification failed: {Error}", probe.Error);
            return probe.PropagateFailure<KeyVerification>();
        }

        public Task<AiResult<JobRequisitionResult>> GenerateJobRequisitionAsync(
            JobRequisitionRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.GenerateJobRequisitionAsync(request, "The custom AI endpoint", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<ServiceHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceHealth>.Ok(new ServiceHealth { Status = "ok", Service = "custom" }));

        public Task<AiResult<ServiceReadiness>> GetReadinessAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceReadiness>.Ok(new ServiceReadiness { Status = "ready", Model = DefaultModel }));

        public Task<AiResult<ServiceVersion>> GetVersionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceVersion>.Ok(new ServiceVersion { Model = DefaultModel }));

        public Task<AiResult<ParseResumeResult>> ExtractResumeAsync(
            Stream content, string fileName, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            return GenericProviderPipeline.ExtractResumeAsync(buffer.ToArray(), fileName, "The custom AI endpoint", Chat(apiKey, baseUriOverride, model), cancellationToken);
        }

        public Task<AiResult<ParseResumeResult>> ExtractResumeByUrlAsync(
            string documentUrl, string? candidateId = null, string? applicationId = null, string? requisitionId = null,
            string? companyId = null, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.ExtractResumeByUrlAsync(_httpClient, documentUrl, "The custom AI endpoint", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<ScreenCandidateResult>> ScreenCandidateAsync(
            ScreenCandidateRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.ScreenCandidateAsync(request, "The custom AI endpoint", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<InterviewQuestionsResult>> GenerateInterviewQuestionsAsync(
            InterviewQuestionsRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.GenerateInterviewQuestionsAsync(request, "The custom AI endpoint", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<CandidateIndexResult>> ListCandidatesAsync(string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<CandidateIndexResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "A custom endpoint has no candidate corpus of its own."));

        public Task<AiResult<RankResult>> RankAsync(string jobDescription, int topK, string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<RankResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "Candidate ranking needs Multinet's embeddings corpus; a custom endpoint has no equivalent."));

        public Task<AiResult<ScoreResult>> ScoreAsync(string profileId, string jobDescription, string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ScoreResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "Rubric scoring needs Multinet's scoring engine; a custom endpoint has no equivalent."));
    }
}
