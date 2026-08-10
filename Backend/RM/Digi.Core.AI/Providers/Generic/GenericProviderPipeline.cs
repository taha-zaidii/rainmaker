using System.Diagnostics;
using Digi.Core.AI.Contracts;

namespace Digi.Core.AI.Providers.Generic
{
    /// <summary>Given a system + user prompt, return the model's raw text, or a failure if the call itself failed.</summary>
    internal delegate Task<AiResult<string>> ChatCompletion(string systemPrompt, string userPrompt, CancellationToken cancellationToken);

    /// <summary>
    /// The orchestration every generic provider shares: build the prompt, call
    /// the model, parse the contract shape, re-assert the advisory invariants.
    /// Only <see cref="ChatCompletion"/> — the actual HTTP call — differs per
    /// vendor, so that is the one thing each provider supplies.
    /// </summary>
    internal static class GenericProviderPipeline
    {
        private static readonly string[] UnverifiedProfileFields =
        {
            "name", "email", "phone", "location", "headline", "summary",
            "spoken_languages", "links", "skills", "education", "experience",
            "projects", "certifications_and_awards",
        };

        public static async Task<AiResult<JobRequisitionResult>> GenerateJobRequisitionAsync(
            JobRequisitionRequest request, string providerName, ChatCompletion chat, CancellationToken cancellationToken)
        {
            var chatResult = await chat(
                GenericPrompts.JobRequisitionSystemPrompt,
                GenericPrompts.BuildJobRequisitionPrompt(request),
                cancellationToken);

            if (chatResult.IsFailure)
            {
                return chatResult.PropagateFailure<JobRequisitionResult>();
            }

            var parsed = LlmJsonSupport.ParseAsContract<JobRequisitionResult>(chatResult.Value!, providerName);
            if (parsed.IsSuccess)
            {
                GenericResultInvariants.Enforce(parsed.Value!, request);
                parsed.Value!.CompanyId = request.CompanyId;
            }

            return parsed;
        }

        public static async Task<AiResult<ParseResumeResult>> ExtractResumeAsync(
            byte[] fileBytes, string fileName, string providerName, ChatCompletion chat, CancellationToken cancellationToken)
        {
            var (success, text, error) = ResumeTextExtractor.TryExtractText(fileBytes, fileName);
            if (!success)
            {
                return AiResult<ParseResumeResult>.Fail(AiErrorCode.UnsupportedFileType, error!);
            }

            var chatResult = await chat(
                GenericPrompts.ResumeExtractionSystemPrompt,
                GenericPrompts.BuildResumeExtractionPrompt(text),
                cancellationToken);

            if (chatResult.IsFailure)
            {
                return chatResult.PropagateFailure<ParseResumeResult>();
            }

            var parsed = LlmJsonSupport.ParseAsContract<ParseResumeResult>(chatResult.Value!, providerName);
            if (parsed.IsSuccess)
            {
                ApplyParseMeta(parsed.Value!);
            }

            return parsed;
        }

        public static async Task<AiResult<ParseResumeResult>> ExtractResumeByUrlAsync(
            HttpClient httpClient, string documentUrl, string providerName, ChatCompletion chat, CancellationToken cancellationToken)
        {
            byte[] fileBytes;
            try
            {
                fileBytes = await httpClient.GetByteArrayAsync(documentUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                return AiResult<ParseResumeResult>.Fail(
                    AiErrorCode.FileProcessingError, $"Could not download the resume from its stored URL: {ex.Message}");
            }

            var fileName = Path.GetFileName(new Uri(documentUrl, UriKind.RelativeOrAbsolute).ToString());
            return await ExtractResumeAsync(fileBytes, fileName, providerName, chat, cancellationToken);
        }

        public static async Task<AiResult<ScreenCandidateResult>> ScreenCandidateAsync(
            ScreenCandidateRequest request, string providerName, ChatCompletion chat, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var chatResult = await chat(
                GenericPrompts.ScreeningSystemPrompt,
                GenericPrompts.BuildScreeningPrompt(request),
                cancellationToken);

            if (chatResult.IsFailure)
            {
                return chatResult.PropagateFailure<ScreenCandidateResult>();
            }

            var parsed = LlmJsonSupport.ParseAsContract<ScreenCandidateResult>(chatResult.Value!, providerName);
            if (parsed.IsSuccess)
            {
                GenericResultInvariants.Enforce(parsed.Value!, request.Threshold, (int)stopwatch.ElapsedMilliseconds);
            }

            return parsed;
        }

        public static async Task<AiResult<InterviewQuestionsResult>> GenerateInterviewQuestionsAsync(
            InterviewQuestionsRequest request, string providerName, ChatCompletion chat, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var chatResult = await chat(
                GenericPrompts.InterviewQuestionsSystemPrompt,
                GenericPrompts.BuildInterviewQuestionsPrompt(request),
                cancellationToken);

            if (chatResult.IsFailure)
            {
                return chatResult.PropagateFailure<InterviewQuestionsResult>();
            }

            var parsed = LlmJsonSupport.ParseAsContract<InterviewQuestionsResult>(chatResult.Value!, providerName);
            if (parsed.IsSuccess)
            {
                GenericResultInvariants.Enforce(parsed.Value!, (int)stopwatch.ElapsedMilliseconds);
            }

            return parsed;
        }

        private static void ApplyParseMeta(ParseResumeResult result)
        {
            result.Meta ??= new ParseMeta();
            result.Meta.SchemaVersion = ProfileSchemaVersions.Supported;
            result.Meta.ExtractionRoute = "text";

            // No generic provider has a verification pipeline behind it the way
            // Multinet's docling+LLM stack does, so every field is marked
            // unverified — the review UI flags all of it rather than this
            // client claiming a confidence it has no basis for.
            foreach (var field in UnverifiedProfileFields)
            {
                result.Meta.FieldProvenance[field] = "llm_unverified";
            }
        }
    }
}
