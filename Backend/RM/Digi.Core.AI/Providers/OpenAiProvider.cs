using System.Net.Http.Headers;
using Digi.Core.AI.Contracts;
using Digi.Core.AI.Providers.Generic;
using Microsoft.Extensions.Logging;

namespace Digi.Core.AI.Providers
{
    /// <summary>
    /// OpenAI's Chat Completions API behind the same <see cref="IAIServiceProvider"/>
    /// contract Multinet's purpose-built service implements. There is no
    /// equivalent on OpenAI's side for a resume-parsing pipeline, a candidate
    /// corpus, or a rubric engine — this provider prompts a general-purpose
    /// model for the same JSON shapes instead (see <c>Providers/Generic</c>) and
    /// declines outright, rather than approximating, whatever it truly has no
    /// basis to answer (<see cref="RankAsync"/>, <see cref="ScoreAsync"/>,
    /// <see cref="ListCandidatesAsync"/>).
    /// </summary>
    public sealed class OpenAiProvider : IAIServiceProvider
    {
        private static readonly Uri DefaultBaseUri = new("https://api.openai.com/v1/");

        // The interface has no per-call model parameter yet (see REFACTOR_STATE.md);
        // until it does, this is the one place a model upgrade happens.
        private const string DefaultModel = "gpt-4o-mini";

        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAiProvider> _logger;

        public OpenAiProvider(HttpClient httpClient, ILogger<OpenAiProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public bool IsStub => false;

        private ChatCompletion Chat(string? apiKey, Uri? baseUriOverride, string? model) => (system, user, ct) =>
            string.IsNullOrWhiteSpace(apiKey)
                ? Task.FromResult(AiResult<string>.Fail(AiErrorCode.RejectedLocally, "No API key configured for OpenAI."))
                : OpenAiCompatibleChat.CompleteAsync(
                    _httpClient, baseUriOverride ?? DefaultBaseUri, apiKey,
                    string.IsNullOrWhiteSpace(model) ? DefaultModel : model, system, user, "OpenAI", ct);

        public async Task<AiResult<KeyVerification>> VerifyKeyAsync(
            string? apiKey = null, Uri? baseUriOverride = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return AiResult<KeyVerification>.Fail(AiErrorCode.RejectedLocally, "No API key configured for OpenAI.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, CombineUrl(baseUriOverride ?? DefaultBaseUri, "models"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return AiResult<KeyVerification>.Ok(new KeyVerification { Valid = true, Service = "openai" });
                }

                return response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? AiResult<KeyVerification>.Fail(AiErrorCode.Unauthorized, "OpenAI rejected this API key.", (int)response.StatusCode)
                    : AiResult<KeyVerification>.Fail(AiErrorCode.InternalError, $"OpenAI returned HTTP {(int)response.StatusCode}.", (int)response.StatusCode, retryable: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI key verification failed to reach the API.");
                return AiResult<KeyVerification>.Fail(AiErrorCode.Unreachable, $"Could not reach OpenAI: {ex.Message}", retryable: true);
            }
        }

        public Task<AiResult<JobRequisitionResult>> GenerateJobRequisitionAsync(
            JobRequisitionRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.GenerateJobRequisitionAsync(request, "OpenAI", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<ServiceHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceHealth>.Ok(new ServiceHealth { Status = "ok", Service = "openai" }));

        public Task<AiResult<ServiceReadiness>> GetReadinessAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceReadiness>.Ok(new ServiceReadiness { Status = "ready", Model = DefaultModel }));

        public Task<AiResult<ServiceVersion>> GetVersionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ServiceVersion>.Ok(new ServiceVersion { Model = DefaultModel }));

        public Task<AiResult<ParseResumeResult>> ExtractResumeAsync(
            Stream content, string fileName, string? apiKey = null, string? model = null, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            return GenericProviderPipeline.ExtractResumeAsync(buffer.ToArray(), fileName, "OpenAI", Chat(apiKey, null, model), cancellationToken);
        }

        public Task<AiResult<ParseResumeResult>> ExtractResumeByUrlAsync(
            string documentUrl, string? candidateId = null, string? applicationId = null, string? requisitionId = null,
            string? companyId = null, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.ExtractResumeByUrlAsync(_httpClient, documentUrl, "OpenAI", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<ScreenCandidateResult>> ScreenCandidateAsync(
            ScreenCandidateRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.ScreenCandidateAsync(request, "OpenAI", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<InterviewQuestionsResult>> GenerateInterviewQuestionsAsync(
            InterviewQuestionsRequest request, string? apiKey = null, Uri? baseUriOverride = null, string? model = null, CancellationToken cancellationToken = default)
            => GenericProviderPipeline.GenerateInterviewQuestionsAsync(request, "OpenAI", Chat(apiKey, baseUriOverride, model), cancellationToken);

        public Task<AiResult<CandidateIndexResult>> ListCandidatesAsync(string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<CandidateIndexResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "OpenAI has no candidate corpus of its own — this portal's stored applicants are the only index."));

        public Task<AiResult<RankResult>> RankAsync(string jobDescription, int topK, string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<RankResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "Candidate ranking needs Multinet's embeddings corpus; OpenAI has no equivalent here."));

        public Task<AiResult<ScoreResult>> ScoreAsync(string profileId, string jobDescription, string? apiKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AiResult<ScoreResult>.Fail(
                AiErrorCode.NotSupportedByProvider, "Rubric scoring needs Multinet's scoring engine; OpenAI has no equivalent here."));

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
