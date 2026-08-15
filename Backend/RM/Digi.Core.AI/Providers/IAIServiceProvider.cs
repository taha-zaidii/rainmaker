using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Digi.Core.AI.Contracts;

namespace Digi.Core.AI.Providers
{
    public interface IAIServiceProvider
    {
        bool IsStub { get; }

        Task<AiResult<KeyVerification>> VerifyKeyAsync(
            string? apiKey = null,
            Uri? baseUriOverride = null,
            CancellationToken cancellationToken = default);

        /// <param name="model">
        /// Overrides the provider's own default model (e.g. the company's saved
        /// "Model" setting). Ignored by Multinet, which pins one resident model
        /// server-side; meaningful for the generic providers, which have no
        /// resident model of their own.
        /// </param>
        Task<AiResult<JobRequisitionResult>> GenerateJobRequisitionAsync(
            JobRequisitionRequest request,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null,
            CancellationToken cancellationToken = default);

        Task<AiResult<ServiceHealth>> GetHealthAsync(CancellationToken cancellationToken = default);

        Task<AiResult<ServiceReadiness>> GetReadinessAsync(CancellationToken cancellationToken = default);

        Task<AiResult<ServiceVersion>> GetVersionAsync(CancellationToken cancellationToken = default);

        Task<AiResult<ParseResumeResult>> ExtractResumeAsync(
            Stream content,
            string fileName,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null,
            CancellationToken cancellationToken = default);

        Task<AiResult<ParseResumeResult>> ExtractResumeByUrlAsync(
            string documentUrl,
            string? candidateId = null,
            string? applicationId = null,
            string? requisitionId = null,
            string? companyId = null,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null,
            CancellationToken cancellationToken = default);

        Task<AiResult<ScreenCandidateResult>> ScreenCandidateAsync(
            ScreenCandidateRequest request,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null,
            CancellationToken cancellationToken = default);

        Task<AiResult<InterviewQuestionsResult>> GenerateInterviewQuestionsAsync(
            InterviewQuestionsRequest request,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null,
            CancellationToken cancellationToken = default);

        Task<AiResult<CandidateIndexResult>> ListCandidatesAsync(
            string? apiKey = null,
            CancellationToken cancellationToken = default);

        Task<AiResult<RankResult>> RankAsync(
            string jobDescription,
            int topK,
            string? apiKey = null,
            CancellationToken cancellationToken = default);

        Task<AiResult<ScoreResult>> ScoreAsync(
            string profileId,
            string jobDescription,
            string? apiKey = null,
            CancellationToken cancellationToken = default);
    }
}
