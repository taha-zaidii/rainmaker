using Digi.Core.AI.Contracts;
using Digi.Core.AI.Configuration;
using Digi.Core.AI.Providers;
using Digi.Recruitment.Module.Domain.Repositories.IRepositories;
using Digi.Recruitment.Module.Domain.Services.IServices;
using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;
using Digi.Shared.SharedLibrary.Interfaces;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Digi.Recruitment.Module.Domain.Services
{
    public class RecruitmentAIService : IRecruitmentAIService
    {
        private readonly IRecruitmentAIRepository _repository;
        private readonly IRecruitmentRepository _recruitmentRepository;
        private readonly ILogger<RecruitmentAIService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IFileStorageService _fileStorageService;

        private readonly IAIServiceProviderResolver _aiResolver;

        public RecruitmentAIService(
            IRecruitmentAIRepository repository,
            IRecruitmentRepository recruitmentRepository,
            ILogger<RecruitmentAIService> logger,
            IHttpClientFactory httpClientFactory,
            IFileStorageService fileStorageService,
            IAIServiceProviderResolver aiResolver)
        {
            _repository = repository;
            _recruitmentRepository = recruitmentRepository;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _fileStorageService = fileStorageService;
            _aiResolver = aiResolver;
        }

        public async Task<ApiResponse<ApiKeyStatusResponseDto>> GetApiKeyStatusAsync(int companyId)
        {
            try
            {
                var result = await _repository.GetApiKeyStatusAsync(companyId);
                if (result == null)
                {
                    return ApiResponse<ApiKeyStatusResponseDto>.Success(
                        new ApiKeyStatusResponseDto { HasApiKey = false, IsValid = false },
                        "API key status retrieved successfully"
                    );
                }

                return ApiResponse<ApiKeyStatusResponseDto>.Success(result, "API key status retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key status for CompanyID: {CompanyID}", companyId);
                return ApiResponse<ApiKeyStatusResponseDto>.Fail($"Error retrieving API key status: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ApiKeySettingsResponseDto>> GetApiKeySettingsAsync(int companyId)
        {
            try
            {
                var result = await _repository.GetApiKeySettingsAsync(companyId);
                if (result == null)
                {
                    return ApiResponse<ApiKeySettingsResponseDto>.Fail("API key settings not found for this company");
                }

                return ApiResponse<ApiKeySettingsResponseDto>.Success(result, "Settings retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key settings for CompanyID: {CompanyID}", companyId);
                return ApiResponse<ApiKeySettingsResponseDto>.Fail($"Error retrieving settings: {ex.Message}");
            }
        }

        public async Task<ApiResponse<SaveApiKeySettingsResponseDto>> SaveApiKeySettingsAsync(SaveApiKeySettingsRequestDto request, string userId)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(request.Provider))
                    return ApiResponse<SaveApiKeySettingsResponseDto>.Fail("Provider is required");

                // "multinetai" is Multinet's own in-house AI service. Without it
                // here, selecting it in the dropdown fails the save outright.
                var validProviders = new[] { "openai", "anthropic", "google", "custom", MultinetAiConstants.Name };
                if (!validProviders.Contains(request.Provider.ToLower()))
                    return ApiResponse<SaveApiKeySettingsResponseDto>.Fail($"Invalid provider. Must be one of: {string.Join(", ", validProviders)}");

                if (string.IsNullOrWhiteSpace(request.ApiKey) || request.ApiKey.Length < 10)
                    return ApiResponse<SaveApiKeySettingsResponseDto>.Fail("API key is required and must be at least 10 characters");

                if (string.IsNullOrWhiteSpace(request.Model))
                    return ApiResponse<SaveApiKeySettingsResponseDto>.Fail("Model is required");

                if (request.MaxTokens < 100 || request.MaxTokens > 4000)
                    return ApiResponse<SaveApiKeySettingsResponseDto>.Fail("MaxTokens must be between 100 and 4000");

                if (request.Temperature < 0 || request.Temperature > 2)
                    return ApiResponse<SaveApiKeySettingsResponseDto>.Fail("Temperature must be between 0 and 2");

                var (id, isSuccess, message) = await _repository.SaveApiKeySettingsAsync(request, userId);
                if (!isSuccess)
                {
                    return ApiResponse<SaveApiKeySettingsResponseDto>.Fail(message);
                }

                return ApiResponse<SaveApiKeySettingsResponseDto>.Success(
                    new SaveApiKeySettingsResponseDto
                    {
                        Id = id ?? 0,
                        CompanyId = request.CompanyId,
                        Provider = request.Provider,
                        UpdatedOn = DateTime.UtcNow
                    },
                    "API key settings saved successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving API key settings for CompanyID: {CompanyID}", request.CompanyId);
                return ApiResponse<SaveApiKeySettingsResponseDto>.Fail($"Error saving API key settings: {ex.Message}");
            }
        }

        public async Task<ApiResponse<TestApiKeyResponseDto>> TestApiKeyAsync(TestApiKeyRequestDto request)
        {
            try
            {
                // Every provider — Multinet, OpenAI, Anthropic, Gemini, and now
                // custom — is tested through the same IAIServiceProvider gateway.
                // "custom" used to have no backend for this at all; it gets one
                // for free here, the same as every other provider.
                var endpoint = request.ApiEndpoint ?? GetDefaultEndpoint(request.Provider);
                return await TestApiKeyViaGatewayAsync(request, endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API key");
                return ApiResponse<TestApiKeyResponseDto>.Fail($"Error testing API key: {ex.Message}");
            }
        }

        // TestOpenAIKeyAsync / TestAnthropicKeyAsync / TestGoogleKeyAsync were
        // removed here — TestApiKeyAsync now tests every provider, including
        // "custom", through OpenAiProvider/AnthropicProvider/GoogleGeminiProvider/
        // CustomAiProvider's own real VerifyKeyAsync (Digi.Core.AI.Providers).

        /// <summary>
        /// Tests a key against the company's configured provider through
        /// <see cref="IAIServiceProvider.VerifyKeyAsync"/> — one path for
        /// Multinet, OpenAI, Anthropic, Gemini and custom endpoints alike.
        ///
        /// Unlike a bare boolean this reports WHY a test failed. "Key rejected"
        /// and "service unreachable" look the same to a recruiter but need
        /// opposite actions, and collapsing both into "API Key Invalid" is what
        /// sent a past round of debugging down the wrong path.
        /// </summary>
        private async Task<ApiResponse<TestApiKeyResponseDto>> TestApiKeyViaGatewayAsync(
            TestApiKeyRequestDto request, string endpoint)
        {
            var resolution = ResolveProviderBaseUri(request.Provider, endpoint);
            if (!resolution.IsUsable)
            {
                return ApiKeyTestFailure(
                    request, TestApiKeyStatus.Misconfigured, resolution.Problem ?? "The API Endpoint is not usable.");
            }

            if (resolution.WasCorrected)
            {
                _logger.LogWarning(
                    "Corrected the stored AI endpoint for CompanyID {CompanyID} before testing: {Warning}",
                    request.CompanyId, resolution.Warning);
            }

            // The settings page lets an administrator leave the key field blank
            // to keep the current key, so "test" most often means "test what is
            // already saved" rather than "test what I just typed".
            var apiKey = request.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                var encryptedApiKey = await _repository.GetEncryptedApiKeyAsync(request.CompanyId);
                if (string.IsNullOrWhiteSpace(encryptedApiKey))
                {
                    return ApiKeyTestFailure(
                        request, TestApiKeyStatus.Misconfigured,
                        "No API key was supplied and none is saved for this company.");
                }

                apiKey = EncryptionHelper.DecryptText(encryptedApiKey);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return ApiKeyTestFailure(
                        request, TestApiKeyStatus.Misconfigured,
                        "The saved API key could not be decrypted. Re-enter and save it.");
                }
            }

            var result = await _aiResolver.Resolve(request.Provider).VerifyKeyAsync(apiKey, resolution.BaseUri);

            if (result.IsSuccess)
            {
                var verification = result.Value!;

                var response = new TestApiKeyResponseDto
                {
                    IsValid = verification.Valid,
                    Provider = request.Provider,
                    Model = verification.Service,
                    ServiceVersion = verification.ServiceVersion,
                    SchemaVersion = verification.SchemaVersion,
                    Capabilities = verification.Capabilities,
                    ConfigurationWarning = resolution.Warning,
                    Status = verification.Valid ? TestApiKeyStatus.Valid : TestApiKeyStatus.InvalidKey,
                    TestResponse = verification.Valid
                        ? $"Connected to {verification.Service ?? "the AI service"} " +
                          $"{verification.ServiceVersion ?? "(version unknown)"}. " +
                          $"{verification.Capabilities.Count} feature(s) available for this key."
                        : null,
                    Error = verification.Valid ? null : "The AI service did not accept this API key."
                };

                if (!verification.Valid)
                {
                    return ApiKeyTestFailure(
                        request, TestApiKeyStatus.InvalidKey, response.Error!, resolution.Warning);
                }

                _logger.LogInformation(
                    "AI key verified for CompanyID {CompanyID}: {Service} {Version}, {Count} capability(ies).",
                    request.CompanyId, verification.Service, verification.ServiceVersion,
                    verification.Capabilities.Count);

                return ApiResponse<TestApiKeyResponseDto>.Success(response, "API key is valid");
            }

            var aiError = result.Error!;

            // A 404 or an unreadable 200 means we reached SOMETHING, just not the
            // AI service — almost always a wrong base URL, which the
            // administrator can fix themselves once we say so.
            var status = aiError.Code switch
            {
                AiErrorCode.Unauthorized => TestApiKeyStatus.InvalidKey,
                AiErrorCode.BadRequest => TestApiKeyStatus.Misconfigured,
                AiErrorCode.ContractViolation => TestApiKeyStatus.Misconfigured,
                AiErrorCode.RejectedLocally => TestApiKeyStatus.Misconfigured,
                _ => TestApiKeyStatus.Unreachable
            };

            _logger.LogWarning(
                "AI key test failed for CompanyID {CompanyID}: {Status} ({Code}, HTTP {HttpStatus}).",
                request.CompanyId, status, aiError.Code, aiError.HttpStatus);

            return ApiKeyTestFailure(request, status, aiError.Message, resolution.Warning);
        }

        /// <summary>
        /// Builds a failed key-test response. Kept in one place so every failure
        /// path carries a machine-readable <see cref="TestApiKeyStatus"/> and the
        /// UI never has to guess from message text.
        ///
        /// Constructed directly rather than through <c>ApiResponse.Fail</c>
        /// because that helper discards the payload, and the payload is the whole
        /// point here — without it the UI is back to a bare boolean and cannot
        /// tell a bad key from an unreachable service. The status code still
        /// makes <c>IsSuccess</c> false, so existing callers behave unchanged.
        /// </summary>
        private static ApiResponse<TestApiKeyResponseDto> ApiKeyTestFailure(
            TestApiKeyRequestDto request, string status, string message, string? configurationWarning = null)
        {
            var failureMessage = status == TestApiKeyStatus.InvalidKey
                ? "Invalid API key or connection failed"
                : "Could not verify the API key";

            var payload = new TestApiKeyResponseDto
            {
                IsValid = false,
                Provider = request.Provider,
                Status = status,
                Error = message,
                ConfigurationWarning = configurationWarning
            };

            return new ApiResponse<TestApiKeyResponseDto>(
                System.Net.HttpStatusCode.BadRequest,
                failureMessage,
                payload,
                new List<string> { message });
        }

        /// <summary>
        /// Base-URL resolution for a call into <see cref="_aiResolver"/>. Multinet
        /// needs its own path-correction logic (a stored ".../api/query" is
        /// rewritten and flagged, since that value 404s) — every other provider
        /// either supplies a plain absolute endpoint or falls back to that
        /// provider's own hardcoded default (see <c>Digi.Core.AI/Providers</c>),
        /// so a null override is a normal, common case, not an error.
        /// </summary>
        private static (bool IsUsable, Uri? BaseUri, string? Problem, string? Warning, bool WasCorrected) ResolveProviderBaseUri(
            string provider, string? apiEndpoint)
        {
            if (MultinetAiConstants.Matches(provider))
            {
                var resolution = MultinetAiEndpoints.ResolveBaseUrl(apiEndpoint);
                return (resolution.IsUsable, resolution.BaseUri, resolution.Problem, resolution.Warning, resolution.WasCorrected);
            }

            if (string.IsNullOrWhiteSpace(apiEndpoint))
            {
                // No override — the provider applies its own default (OpenAI,
                // Anthropic, Gemini) or fails cleanly itself if it has none ("custom").
                return (true, null, null, null, false);
            }

            return Uri.TryCreate(apiEndpoint.Trim(), UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? (true, uri, null, null, false)
                : (false, null, $"The configured API endpoint '{apiEndpoint}' is not a usable absolute http(s) URL.", null, false);
        }

        // Still used by the generic-prompt features that have no IAIServiceProvider
        // equivalent yet (candidate matching/ranking, interview scheduling,
        // salary recommendations) — see CallAIAPIAsync and its call sites below.
        private string GetDefaultEndpoint(string provider)
        {
            return provider.ToLower() switch
            {
                "openai" => "https://api.openai.com/v1",
                "anthropic" => "https://api.anthropic.com/v1",
                "google" => "https://generativelanguage.googleapis.com/v1",

                // Base URL only — the backend appends the per-feature path.
                // There is no /api/query endpoint; that value 404s.
                "multinetai" => "https://ai.rainmaker.pk/hrms/api/v1",

                _ => "https://api.openai.com/v1"
            };
        }

        public async Task<ApiResponse<bool>> DeleteApiKeyAsync(int companyId)
        {
            try
            {
                var result = await _repository.DeleteApiKeyAsync(companyId);
                if (result)
                {
                    return ApiResponse<bool>.Success(true, "API key deleted successfully");
                }
                return ApiResponse<bool>.Fail("Failed to delete API key");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API key for CompanyID: {CompanyID}", companyId);
                return ApiResponse<bool>.Fail($"Error deleting API key: {ex.Message}");
            }
        }

        public async Task<ApiResponse<DashboardStatsResponseDto>> GetDashboardStatsAsync(int companyId)
        {
            try
            {
                var result = await _repository.GetDashboardStatsAsync(companyId);
                if (result == null)
                {
                    return ApiResponse<DashboardStatsResponseDto>.Success(
                        new DashboardStatsResponseDto(),
                        "Statistics retrieved successfully"
                    );
                }

                return ApiResponse<DashboardStatsResponseDto>.Success(result, "Statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats for CompanyID: {CompanyID}", companyId);
                return ApiResponse<DashboardStatsResponseDto>.Fail($"Error retrieving statistics: {ex.Message}");
            }
        }

        public async Task<ApiResponse<GenerateJobDescriptionResponseDto>> GenerateJobDescriptionAsync(GenerateJobDescriptionRequestDto request)
        {
            try
            {
                // Get API settings
                var settings = await _repository.GetApiKeySettingsAsync(request.CompanyId);
                if (settings == null)
                {
                    return ApiResponse<GenerateJobDescriptionResponseDto>.Fail("API key not configured");
                }

                // Get actual encrypted API key from database
                var encryptedApiKey = await _repository.GetEncryptedApiKeyAsync(request.CompanyId);
                if (string.IsNullOrWhiteSpace(encryptedApiKey))
                {
                    return ApiResponse<GenerateJobDescriptionResponseDto>.Fail("API key not found in database");
                }

                // Decrypt API key
                var decryptedApiKey = EncryptionHelper.DecryptText(encryptedApiKey);
                if (string.IsNullOrWhiteSpace(decryptedApiKey))
                {
                    return ApiResponse<GenerateJobDescriptionResponseDto>.Fail("Invalid API key - decryption failed");
                }

                // Every provider generates through the same IAIServiceProvider
                // gateway now — Multinet's purpose-built endpoint, and OpenAI /
                // Anthropic / Gemini / custom prompted for the identical
                // structured JSON shape (see Digi.Core.AI/Providers/Generic).
                // The response mapping below never needed to know which one answered.
                return await GenerateJobDescriptionViaGatewayAsync(request, settings, decryptedApiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating job description for CompanyID: {CompanyID}", request.CompanyId);
                return ApiResponse<GenerateJobDescriptionResponseDto>.Fail($"Error generating job description: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates a job requisition draft through the company's configured
        /// AI provider — Multinet's purpose-built endpoint, or a general-purpose
        /// model prompted for the identical structured shape (see
        /// <c>Digi.Core.AI.Providers.Generic</c>). Either way the result is a
        /// structured 4-step draft, not prose.
        ///
        /// The draft is ADVISORY throughout: every field is pre-filled and
        /// editable, several are deliberately left empty for HR, and the status
        /// is always Draft. Nothing here may auto-commit a requisition.
        /// </summary>
        private async Task<ApiResponse<GenerateJobDescriptionResponseDto>> GenerateJobDescriptionViaGatewayAsync(
            GenerateJobDescriptionRequestDto request,
            ApiKeySettingsResponseDto settings,
            string apiKey)
        {
            var resolution = ResolveProviderBaseUri(settings.Provider, settings.ApiEndpoint);
            if (!resolution.IsUsable)
            {
                return ApiResponse<GenerateJobDescriptionResponseDto>.Fail(
                    resolution.Problem ?? "The AI service endpoint is not configured correctly.");
            }

            if (resolution.Warning is not null)
            {
                _logger.LogWarning(
                    "AI endpoint for CompanyID {CompanyID} needs attention: {Warning}",
                    request.CompanyId, resolution.Warning);
            }

            // Placeholders are stripped rather than forwarded. The service treats
            // what it receives as a fact a human asserted, so a literal "N/A" in
            // the experience field becomes a real constraint on the output.
            var aiRequest = new JobRequisitionRequest
            {
                CompanyId = request.CompanyId,
                JobTitle = MultinetAiText.Clean(request.JobTitle) ?? string.Empty,
                Department = MultinetAiText.Clean(request.Department),
                Designation = MultinetAiText.Clean(request.Designation),
                ExperienceRequired = MultinetAiText.Clean(request.Experience),
                KeySkills = MultinetAiText.Clean(request.Skills),
                JobCategoryOptions = MultinetAiText.Clean(request.JobCategoryOptions),
                AdditionalContext = MultinetAiText.Clean(request.AdditionalInfo)
            };

            if (string.IsNullOrWhiteSpace(aiRequest.JobTitle))
            {
                return ApiResponse<GenerateJobDescriptionResponseDto>.Fail(
                    "A job title is required before a job description can be generated.");
            }

            if (aiRequest.JobCategoryOptions is null)
            {
                // Not fatal, but the category then comes back as free text that
                // the wizard's dropdown may refuse to bind.
                _logger.LogInformation(
                    "No job category options were supplied for CompanyID {CompanyID}; the AI cannot snap " +
                    "its answer to a selectable value.", request.CompanyId);
            }

            var result = await _aiResolver.Resolve(settings.Provider).GenerateJobRequisitionAsync(
                aiRequest, apiKey, resolution.BaseUri, model: settings.Model);

            if (result.IsFailure)
            {
                var aiError = result.Error!;
                _logger.LogWarning(
                    "Job description generation failed for CompanyID {CompanyID}: {Code} (HTTP {Status}).",
                    request.CompanyId, aiError.Code, aiError.HttpStatus);

                return ApiResponse<GenerateJobDescriptionResponseDto>.Fail(aiError.Message);
            }

            var generated = result.Value!;
            var draft = MapJobRequisitionDraft(generated);
            var rendered = RenderJobDescription(draft);

            var response = new GenerateJobDescriptionResponseDto
            {
                JobDescription = rendered,
                GeneratedOn = DateTime.UtcNow,

                // The service reports wall-clock time, not token counts — it runs
                // a resident local model with no per-token cost to meter.
                TokensUsed = 0,
                Model = settings.Model,

                Draft = draft,
                ReviewRequired = generated.ReviewRequired,
                ExecutionTimeMs = generated.ExecutionTimeMs,
                CacheHit = generated.Meta?.CacheHit,
                ExperienceSource = generated.Meta?.ExperienceSource,
                JobCategorySource = generated.Meta?.JobCategorySource,
                WorkMode = generated.Meta?.WorkMode,
                FieldsForHumanToComplete = DescribeFieldsLeftForHuman(draft)
            };

            // Persisted through the same repository calls as every other
            // provider, so history and the activity feed stay consistent. The
            // request payload stands in for the prompt: it is what was actually
            // sent, and it is what the AI team needs to reproduce a bad result.
            await _repository.SaveJobDescriptionAsync(
                request.CompanyId,
                null,
                rendered,
                JsonSerializer.Serialize(aiRequest),
                settings.Model ?? "",
                0,
                "System"
            );

            await _repository.SaveActivityAsync(
                request.CompanyId,
                "job_description",
                "Job Description Generated",
                $"Generated job description for {request.JobTitle}",
                null
            );

            return ApiResponse<GenerateJobDescriptionResponseDto>.Success(
                response, "Job description generated successfully");
        }

        /// <summary>
        /// Maps the AI service's wire contract onto the portal's wizard DTOs.
        ///
        /// Nulls are copied through untouched and never defaulted. A null here
        /// means "the AI is forbidden from deciding this" — substituting a value
        /// would put words in HR's mouth, and in the case of age limits would
        /// manufacture a discriminatory constraint out of nothing.
        /// </summary>
        private static AiJobDraftDto MapJobRequisitionDraft(JobRequisitionResult generated)
        {
            var basicInfo = generated.Data?.BasicInfo;
            var requirements = generated.Data?.Requirements;
            var compensation = generated.Data?.Compensation;
            var publishing = generated.Data?.Publishing;

            return new AiJobDraftDto
            {
                BasicInfo = new AiJobDraftBasicInfoDto
                {
                    JobTitle = basicInfo?.JobTitle,
                    Department = basicInfo?.Department,
                    Designation = basicInfo?.Designation,
                    JobSummary = basicInfo?.JobSummary,
                    JobCategory = basicInfo?.JobCategory,

                    // Always 1 from the service; defaulted only if absent entirely.
                    Vacancies = basicInfo?.Vacancies ?? 1,
                    EmploymentType = basicInfo?.EmploymentType,
                    Grade = basicInfo?.Grade
                },
                Requirements = new AiJobDraftRequirementsDto
                {
                    ExperienceYears = MapRange(requirements?.ExperienceYears),
                    AgeLimits = MapRange(requirements?.AgeLimits),
                    KeyResponsibilities = requirements?.KeyResponsibilities ?? new List<string>(),
                    Requirements = requirements?.Requirements ?? new List<string>(),
                    Qualifications = requirements?.Qualifications ?? new List<string>(),
                    Skills = requirements?.Skills ?? new List<string>()
                },
                Compensation = new AiJobDraftCompensationDto
                {
                    Location = compensation?.Location,
                    Benefits = compensation?.Benefits,
                    BudgetType = compensation?.BudgetType,
                    BudgetLineId = compensation?.BudgetLineId
                },
                Publishing = new AiJobDraftPublishingDto
                {
                    Justification = publishing?.Justification,

                    // A human publishes. Absent is treated as false, never as true.
                    IsPublicJob = publishing?.IsPublicJob ?? false,
                    Status = string.IsNullOrWhiteSpace(publishing?.Status) ? "Draft" : publishing.Status,
                    ClosingDate = publishing?.ClosingDate
                }
            };
        }

        /// <summary>Null in, null out — an empty range is not the same as a zero one.</summary>
        private static AiJobDraftRangeDto? MapRange(JobRequisitionRange? range) =>
            range is null || !range.HasValue
                ? null
                : new AiJobDraftRangeDto { Minimum = range.Minimum, Maximum = range.Maximum };

        /// <summary>
        /// Names the fields the AI deliberately left empty, so the UI can present
        /// them as "for you to complete" rather than as a half-failed generation.
        /// </summary>
        private static List<string> DescribeFieldsLeftForHuman(AiJobDraftDto draft)
        {
            var pending = new List<string>();

            if (string.IsNullOrWhiteSpace(draft.BasicInfo.EmploymentType)) pending.Add("Employment Type");
            if (string.IsNullOrWhiteSpace(draft.BasicInfo.Grade)) pending.Add("Grade");
            if (string.IsNullOrWhiteSpace(draft.Compensation.Benefits)) pending.Add("Benefits");
            if (string.IsNullOrWhiteSpace(draft.Compensation.BudgetType)) pending.Add("Budget Type");
            if (draft.Compensation.BudgetLineId is null) pending.Add("Budget Line");
            if (string.IsNullOrWhiteSpace(draft.Publishing.Justification)) pending.Add("Justification");
            if (string.IsNullOrWhiteSpace(draft.Publishing.ClosingDate)) pending.Add("Closing Date");

            return pending;
        }

        /// <summary>
        /// Renders the structured draft as readable text.
        ///
        /// This exists so callers that only understand a single job-description
        /// blob — the current portal among them — keep working unchanged. New
        /// screens should bind the structured draft field by field instead, which
        /// is the whole reason the AI service returns steps rather than prose.
        /// </summary>
        private static string RenderJobDescription(AiJobDraftDto draft)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(draft.BasicInfo.JobTitle))
            {
                sb.AppendLine(draft.BasicInfo.JobTitle);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(draft.BasicInfo.JobSummary))
            {
                sb.AppendLine(draft.BasicInfo.JobSummary);
                sb.AppendLine();
            }

            var experience = draft.Requirements.ExperienceYears;
            if (experience is not null)
            {
                var range = experience.Minimum.HasValue && experience.Maximum.HasValue
                    ? $"{experience.Minimum}-{experience.Maximum} years"
                    : experience.Minimum.HasValue
                        ? $"{experience.Minimum}+ years"
                        : $"Up to {experience.Maximum} years";

                sb.AppendLine($"Experience Required: {range}");
            }

            if (!string.IsNullOrWhiteSpace(draft.Compensation.Location))
            {
                sb.AppendLine($"Location: {draft.Compensation.Location}");
            }

            AppendSection(sb, "Key Responsibilities", draft.Requirements.KeyResponsibilities);
            AppendSection(sb, "Requirements", draft.Requirements.Requirements);
            AppendSection(sb, "Qualifications", draft.Requirements.Qualifications);
            AppendSection(sb, "Skills", draft.Requirements.Skills);

            return sb.ToString().TrimEnd();
        }

        private static void AppendSection(StringBuilder sb, string heading, List<string> items)
        {
            if (items is null || items.Count == 0)
            {
                return;
            }

            sb.AppendLine();
            sb.AppendLine(heading);
            foreach (var item in items)
            {
                sb.AppendLine($"- {item}");
            }
        }

        public async Task<ApiResponse<SaveJobDescriptionResponseDto>> SaveJobDescriptionAsync(SaveJobDescriptionRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return ApiResponse<SaveJobDescriptionResponseDto>.Fail("Request is required");
                }

                if (string.IsNullOrWhiteSpace(request.JobDescription))
                {
                    return ApiResponse<SaveJobDescriptionResponseDto>.Fail("Job description is required");
                }

                var userId = "System"; // Get from context if available
                var (id, isUpdate, isSuccess, message, jobRequisitionId) = await _repository.SaveJobDescriptionWithUpdateAsync(request, userId);

                if (!isSuccess)
                {
                    return ApiResponse<SaveJobDescriptionResponseDto>.Fail(message);
                }

                return ApiResponse<SaveJobDescriptionResponseDto>.Success(
                    new SaveJobDescriptionResponseDto
                    {
                        Id = id ?? 0,
                        JobRequisitionId = jobRequisitionId, // Return the actual Job Requisition ID (created or existing)
                        IsUpdate = isUpdate,
                        Saved = true
                    },
                    message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving job description");
                return ApiResponse<SaveJobDescriptionResponseDto>.Fail($"Error saving job description: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ScreenResumeResponseDto>> ScreenResumeAsync(ScreenResumeRequestDto request)
        {
            try
            {
                // Validation
                if (request == null)
                {
                    return ApiResponse<ScreenResumeResponseDto>.Fail("Request body is required");
                }

                if (request.JobRequirements == null)
                {
                    return ApiResponse<ScreenResumeResponseDto>.Fail("JobRequirements is required");
                }

                // Extract resume text from file path (backend handles text extraction)
                string resumeText = string.Empty;
                
                if (string.IsNullOrWhiteSpace(request.ResumeFilePath))
                {
                    return ApiResponse<ScreenResumeResponseDto>.Fail("ResumeFilePath is required");
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(request.ResumeFilePath))
                    {
                        resumeText = await ExtractTextFromResumeFileAsync(request.ResumeFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not extract text from file {FilePath}, falling back to candidate profile in DB", request.ResumeFilePath);
                }

                // Fall back to candidate profile in database if file text is empty or unreadable
                if (string.IsNullOrWhiteSpace(resumeText) && request.ApplicationID.HasValue && request.ApplicationID.Value > 0)
                {
                    var app = await _recruitmentRepository.GetJobApplicationByIdAsync(request.ApplicationID.Value);
                    if (app != null)
                    {



                        var sb = new StringBuilder();
                        sb.AppendLine($"Candidate Name: {app.FullName}");
                        sb.AppendLine($"Email: {app.Email}");
                        sb.AppendLine($"Current Designation: {app.CurrentDesignation ?? app.CurrentJobTitle}");
                        sb.AppendLine($"Current Company: {app.CurrentCompany}");
                        sb.AppendLine($"Experience Years: {app.ExperienceYears ?? app.TotalExperience}");
                        sb.AppendLine($"Skills: {app.Skills}");
                        sb.AppendLine($"Education: {app.Education}");
                        sb.AppendLine($"Experience Summary: {app.ExperienceSummary}");
                        sb.AppendLine($"Cover Letter: {app.CoverLetter}");
                        resumeText = sb.ToString();
                    }
                }

                if (string.IsNullOrWhiteSpace(resumeText))
                {
                    return ApiResponse<ScreenResumeResponseDto>.Fail("No profile data or resume text available for AI screening.");
                }

                request.ResumeText = resumeText;


                // Get API settings
                var settings = await _repository.GetApiKeySettingsAsync(request.CompanyId);
                if (settings == null)
                {
                    return ApiResponse<ScreenResumeResponseDto>.Fail("API key not configured. Please configure your AI provider settings first.");
                }

                // Unlike AutoParse (which only gates the system-initiated call),
                // Auto Resume Screening gates every screening call, including the
                // recruiter's own "Run AI Screening" button — there is no
                // system-initiated screening call site today, so this toggle
                // would otherwise control nothing.
                if (settings.Settings?.AutoScreening == false)
                {
                    return ApiResponse<ScreenResumeResponseDto>.Fail(
                        "AI Resume Screening is currently disabled. Enable it in AI Settings to score this candidate.");
                }

                // Get actual encrypted API key from database
                var encryptedApiKey = await _repository.GetEncryptedApiKeyAsync(request.CompanyId);
                if (string.IsNullOrWhiteSpace(encryptedApiKey))
                {
                    return ApiResponse<ScreenResumeResponseDto>.Fail("API key not found in database");
                }

                // Decrypt API key
                var decryptedApiKey = EncryptionHelper.DecryptText(encryptedApiKey);
                if (string.IsNullOrWhiteSpace(decryptedApiKey))
                {
                    return ApiResponse<ScreenResumeResponseDto>.Fail("Invalid API key - decryption failed");
                }

                // Screening now runs through the same IAIServiceProvider gateway
                // for every provider — Multinet's purpose-built endpoint, or a
                // general-purpose model prompted for the identical
                // ScreenCandidateResult shape (Digi.Core.AI.Providers.Generic).
                {
                    var resolution = ResolveProviderBaseUri(settings.Provider, settings.ApiEndpoint);
                    if (!resolution.IsUsable)
                    {
                        return ApiResponse<ScreenResumeResponseDto>.Fail(
                            resolution.Problem ?? "The AI provider's endpoint is not configured correctly.");
                    }

                    var threshold = settings.AutoShortlistThreshold > 0 ? settings.AutoShortlistThreshold : 80;

                    JobApplicationResponseDto? appDetails = null;
                    if (request.ApplicationID.HasValue && request.ApplicationID.Value > 0)
                    {
                        appDetails = await _recruitmentRepository.GetJobApplicationByIdAsync(request.ApplicationID.Value);
                    }

                    var reqRequirements = new List<string>();
                    if (!string.IsNullOrWhiteSpace(request.JobRequirements.Education))
                    {
                        reqRequirements.Add($"Education: {request.JobRequirements.Education}");
                    }
                    if (request.JobRequirements.RequiredSkills != null && request.JobRequirements.RequiredSkills.Any())
                    {
                        reqRequirements.Add($"Required Skills: {string.Join(", ", request.JobRequirements.RequiredSkills)}");
                    }

                    var keySkillsList = request.JobRequirements.RequiredSkills ?? new List<string>();

                    var candProfile = new CandidateProfile
                    {
                        Name = appDetails?.FullName ?? "Applicant",
                        Email = appDetails?.Email,
                        Phone = appDetails?.MobileNumber,
                        Location = appDetails?.CurrentAddress ?? appDetails?.PreferredLocation,
                        Headline = appDetails?.CurrentDesignation ?? appDetails?.CurrentJobTitle,
                        Summary = !string.IsNullOrWhiteSpace(appDetails?.ExperienceSummary) ? appDetails.ExperienceSummary : (!string.IsNullOrWhiteSpace(appDetails?.CoverLetter) ? appDetails.CoverLetter : resumeText),
                        Skills = !string.IsNullOrWhiteSpace(appDetails?.Skills)
                            ? appDetails.Skills.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                            : keySkillsList,
                        Education = new List<EducationNode>
                        {
                            new EducationNode
                            {
                                Degree = appDetails?.Education ?? request.JobRequirements.Education ?? "Bachelor Degree"
                            }
                        },
                        Experience = new List<ExperienceNode>
                        {
                            new ExperienceNode
                            {
                                Company = appDetails?.CurrentCompany ?? "Current Employer",
                                Role = appDetails?.CurrentDesignation ?? appDetails?.CurrentJobTitle ?? request.JobRequirements.JobTitle ?? "Software Engineer",
                                Duration = $"{appDetails?.ExperienceYears ?? appDetails?.TotalExperience ?? 3} years"
                            }
                        }
                    };

                    var multinetReq = new ScreenCandidateRequest
                    {
                        CandidateProfile = candProfile,
                        JobTitle = appDetails?.RequisitionJobTitle ?? request.JobRequirements.JobTitle ?? "Position",
                        JobRequirements = reqRequirements,
                        KeySkills = keySkillsList,
                        ExperienceRequired = request.JobRequirements.Experience,

                        Threshold = threshold,
                        ApplicationId = request.ApplicationID?.ToString(),
                        CandidateId = appDetails?.ApplicantID.ToString() ?? request.ApplicantID?.ToString()
                    };


                    var startTimeAi = DateTime.UtcNow;
                    var aiResult = await _aiResolver.Resolve(settings.Provider).ScreenCandidateAsync(
                        multinetReq, decryptedApiKey, resolution.BaseUri, model: settings.Model).ConfigureAwait(false);

                    var processingTimeMs = (int)(DateTime.UtcNow - startTimeAi).TotalMilliseconds;

                    if (aiResult.IsFailure)
                    {
                        return ApiResponse<ScreenResumeResponseDto>.Fail(
                            $"AI screening failed: {aiResult.Error?.Message}");
                    }

                    var screened = aiResult.Value!;

                    var responseDto = new ScreenResumeResponseDto
                    {
                        MatchScore = screened.MatchScore,
                        Recommendation = screened.Shortlisted ? "Recommended for Shortlist" : "Under Review",
                        Strengths = screened.MatchedSkills,
                        Weaknesses = screened.MissingSkills,
                        ScreeningNotes = string.Join("\n", screened.Reasons.Select(r => $"[{r.Kind.ToUpper()}] {r.Detail} (Evidence: {r.Evidence})")),
                        ScreenedOn = DateTime.UtcNow,
                        Shortlisted = screened.Shortlisted,
                        ThresholdUsed = screened.ThresholdUsed,
                        MatchedSkills = screened.MatchedSkills,
                        MissingSkills = screened.MissingSkills,
                        Reasons = screened.Reasons.Select(r => new ScreeningReasonDto
                        {
                            Kind = r.Kind,
                            Detail = r.Detail,
                            Evidence = r.Evidence
                        }).ToList()
                    };

                    await _repository.SaveResumeScreeningAsync(
                        companyId: request.CompanyId,
                        applicationId: request.ApplicationID,
                        applicantId: request.ApplicantID,
                        resumeParsingId: request.ResumeParsingID,
                        matchScore: screened.MatchScore,
                        skillsMatch: string.Join("; ", screened.MatchedSkills),
                        experienceMatch: screened.Shortlisted ? "Shortlisted by threshold" : "Review needed",
                        qualificationsMatch: $"Match score: {screened.MatchScore}/{threshold}",
                        redFlags: string.Join("; ", screened.MissingSkills),
                        recommendation: responseDto.Recommendation,
                        screeningMethod: "AI",
                        screeningProvider: settings.Provider,
                        modelUsed: settings.Model ?? "qwen3.5:27b",
                        processingTime: processingTimeMs,
                        userId: "System"
                    );

                    // Dynamic 3-tier status (SHORTLISTED / SCREENING / REJECTED by
                    // score band) rather than a hardcoded 2-status split — this was
                    // previously only applied on the legacy generic-provider path;
                    // every provider gets the same, more complete behavior now.
                    if (request.ApplicationID.HasValue && request.ApplicationID.Value > 0)
                    {
                        var statusesResult = await _recruitmentRepository.GetStatusesByTypeAsync("APPLICATION", request.CompanyId);
                        if (statusesResult == null || !statusesResult.Any())
                        {
                            return ApiResponse<ScreenResumeResponseDto>.Fail("Application statuses not configured");
                        }

                        var targetStatusCode = screened.MatchScore >= 70
                            ? "SHORTLISTED"
                            : screened.MatchScore >= 40
                                ? "SCREENING"
                                : "REJECTED";

                        var targetStatus = statusesResult.FirstOrDefault(x =>
                            x.StatusCode.Equals(targetStatusCode, StringComparison.OrdinalIgnoreCase) && x.IsActive);

                        if (targetStatus == null)
                        {
                            return ApiResponse<ScreenResumeResponseDto>.Fail($"{targetStatusCode} status not configured");
                        }

                        await _recruitmentRepository.UpdateApplicationStatusOnlyAsync(
                            request.ApplicationID.Value,
                            targetStatus.StatusID,
                            screened.MatchScore,
                            screened.MatchScore / 20,
                            "System"
                        );
                    }

                    await _repository.SaveActivityAsync(
                        request.CompanyId,
                        "resume_screening",
                        "Resume Screened",
                        $"Screened resume for {request.JobRequirements.JobTitle} via {settings.Provider} (Score: {screened.MatchScore})",
                        request.ResumeId
                    );

                    return ApiResponse<ScreenResumeResponseDto>.Success(responseDto, "Resume screened successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error screening resume for CompanyID: {CompanyID}", request.CompanyId);
                return ApiResponse<ScreenResumeResponseDto>.Fail($"Error screening resume: {ex.Message}");
            }
        }

        public async Task<ApiResponse<MatchCandidateResponseDto>> MatchCandidateAsync(MatchCandidateRequestDto request)
        {
            try
            {
                // Get API settings
                var settings = await _repository.GetApiKeySettingsAsync(request.CompanyId);
                if (settings == null)
                {
                    return ApiResponse<MatchCandidateResponseDto>.Fail("API key not configured");
                }

                // Get actual encrypted API key from database
                var encryptedApiKey = await _repository.GetEncryptedApiKeyAsync(request.CompanyId);
                if (string.IsNullOrWhiteSpace(encryptedApiKey))
                {
                    return ApiResponse<MatchCandidateResponseDto>.Fail("API key not found in database");
                }

                // Decrypt API key
                var decryptedApiKey = EncryptionHelper.DecryptText(encryptedApiKey);
                if (string.IsNullOrWhiteSpace(decryptedApiKey))
                {
                    return ApiResponse<MatchCandidateResponseDto>.Fail("Invalid API key - decryption failed");
                }

                // Build prompt
                var prompt = BuildMatchingPrompt(request);

                // Call AI API
                var (isSuccess, aiResponse, tokensUsed, error) = await CallAIAPIAsync(
                    settings.Provider,
                    decryptedApiKey,
                    settings.ApiEndpoint,
                    settings.Model ?? "gpt-3.5-turbo",
                    prompt,
                    settings.MaxTokens,
                    settings.Temperature
                );

                if (!isSuccess)
                {
                    // Check if it's a quota/rate limit error
                    var isQuotaError = error?.Contains("quota", StringComparison.OrdinalIgnoreCase) == true ||
                                      error?.Contains("rate limit", StringComparison.OrdinalIgnoreCase) == true ||
                                      error?.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase) == true;
                    
                    var errorMessage = isQuotaError 
                        ? error 
                        : $"Failed to match candidate: {error}";
                    
                    return ApiResponse<MatchCandidateResponseDto>.Fail(errorMessage);
                }

                // Parse AI response
                var matchResult = ParseMatchingResponse(aiResponse ?? "", request);
                
               await _repository.SaveCandidateAIMatchAsync(request.CompanyId,(int)request.JobRequisitionId,(int)request.CandidateId, matchResult,"AI");

                return ApiResponse<MatchCandidateResponseDto>.Success(
                    matchResult,
                    "Candidate matched successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error matching candidate for CompanyID: {CompanyID}", request.CompanyId);
                return ApiResponse<MatchCandidateResponseDto>.Fail($"Error matching candidate: {ex.Message}");
            }
        }

        public async Task<ApiResponse<GenerateInterviewQuestionsResponseDto>> GenerateInterviewQuestionsAsync(GenerateInterviewQuestionsRequestDto request)
        {
            try
            {
                // Get API settings
                var settings = await _repository.GetApiKeySettingsAsync(request.CompanyId);
                if (settings == null)
                {
                    return ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail("API key not configured");
                }

                // "Generate Interview Questions" has no manual/automatic split
                // like resume parsing does — there is no button anywhere yet
                // that calls this, so unlike ParseResumeAsync there is no
                // legitimate "a person explicitly asked for this" case to
                // exempt. The whole feature is off when the setting is off.
                if (settings.Settings?.GenerateQuestions == false)
                {
                    return ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail(
                        "Interview question generation is turned off for this company.");
                }

                // Get actual encrypted API key from database
                var encryptedApiKey = await _repository.GetEncryptedApiKeyAsync(request.CompanyId);
                if (string.IsNullOrWhiteSpace(encryptedApiKey))
                {
                    return ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail("API key not found in database");
                }

                // Decrypt API key
                var decryptedApiKey = EncryptionHelper.DecryptText(encryptedApiKey);
                if (string.IsNullOrWhiteSpace(decryptedApiKey))
                {
                    return ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail("Invalid API key - decryption failed");
                }

                // Every provider generates through the same IAIServiceProvider
                // gateway now — Multinet's purpose-built endpoint, or a
                // general-purpose model prompted for the identical
                // InterviewQuestionsResult shape (Digi.Core.AI.Providers.Generic).
                var resolution = ResolveProviderBaseUri(settings.Provider, settings.ApiEndpoint);
                if (!resolution.IsUsable)
                {
                    return ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail(
                        resolution.Problem ?? "The AI provider's endpoint is not configured correctly.");
                }

                var keySkillsList = request.JobRequirements?.RequiredSkills ?? new List<string>();

                var aiRequest = new InterviewQuestionsRequest
                {
                    JobTitle = request.JobTitle,
                    JobDescription = request.JobRequirements?.Education ?? string.Empty,
                    KeySkills = keySkillsList,
                    QuestionsPerCategory = request.NumberOfQuestions > 0 ? request.NumberOfQuestions : 5,
                    Categories = new List<string> { request.QuestionType.ToLower() == "mixed" ? "technical" : request.QuestionType.ToLower(), "behavioral", "role_specific" }
                };

                var aiResult = await _aiResolver.Resolve(settings.Provider).GenerateInterviewQuestionsAsync(
                    aiRequest, decryptedApiKey, resolution.BaseUri, model: settings.Model).ConfigureAwait(false);

                if (aiResult.IsFailure)
                {
                    return ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail(
                        $"Interview questions generation failed: {aiResult.Error?.Message}");
                }

                var generated = aiResult.Value!;
                var questionDtos = new List<InterviewQuestionDto>();

                if (generated.QuestionBank != null)
                {
                    foreach (var categoryPair in generated.QuestionBank)
                    {
                        var category = categoryPair.Key;
                        foreach (var item in categoryPair.Value)
                        {
                            questionDtos.Add(new InterviewQuestionDto
                            {
                                Question = item.Question,
                                Type = category,
                                Category = category,
                                ExpectedAnswer = item.WhatToListFor
                            });
                        }
                    }
                }

                return ApiResponse<GenerateInterviewQuestionsResponseDto>.Success(
                    new GenerateInterviewQuestionsResponseDto
                    {
                        Questions = questionDtos,
                        GeneratedOn = DateTime.UtcNow,
                        TokensUsed = 0
                    },
                    "Interview questions generated successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating interview questions for CompanyID: {CompanyID}", request.CompanyId);
                return ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail($"Error generating interview questions: {ex.Message}");
            }
        }

        public async Task<ApiResponse<SaveSettingsResponseDto>> SaveSettingsAsync(SaveSettingsRequestDto request)
        {
            try
            {
                var (isSuccess, message) = await _repository.SaveSettingsAsync(request);
                if (!isSuccess)
                {
                    return ApiResponse<SaveSettingsResponseDto>.Fail(message);
                }

                return ApiResponse<SaveSettingsResponseDto>.Success(
                    new SaveSettingsResponseDto
                    {
                        CompanyId = request.CompanyId,
                        Settings = request.Settings,
                        UpdatedOn = DateTime.UtcNow
                    },
                    "Settings saved successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings for CompanyID: {CompanyID}", request.CompanyId);
                return ApiResponse<SaveSettingsResponseDto>.Fail($"Error saving settings: {ex.Message}");
            }
        }

        public async Task<ApiResponse<GetSettingsResponseDto>> GetSettingsAsync(int companyId)
        {
            try
            {
                var result = await _repository.GetSettingsAsync(companyId);
                if (result == null)
                {
                    return ApiResponse<GetSettingsResponseDto>.Success(
                        new GetSettingsResponseDto
                        {
                            CompanyId = companyId,
                            Settings = new FeatureSettingsDto()
                        },
                        "Settings retrieved successfully"
                    );
                }

                return ApiResponse<GetSettingsResponseDto>.Success(result, "Settings retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting settings for CompanyID: {CompanyID}", companyId);
                return ApiResponse<GetSettingsResponseDto>.Fail($"Error retrieving settings: {ex.Message}");
            }
        }

        // Helper methods
        // BuildJobDescriptionPrompt was removed here — JD generation now goes
        // through GenerateJobDescriptionViaGatewayAsync for every provider,
        // which prompts via Digi.Core.AI.Providers.Generic.GenericPrompts instead.

        // BuildResumeScreeningPrompt was removed here — screening now goes
        // through the same IAIServiceProvider gateway for every provider (see
        // ScreenResumeAsync), which prompts via
        // Digi.Core.AI.Providers.Generic.GenericPrompts instead.

        private string BuildMatchingPrompt(MatchCandidateRequestDto request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Match the candidate profile to the job requirements:");
            sb.AppendLine("\nCANDIDATE PROFILE:");
            if (request.CandidateProfile.Skills.Any())
                sb.AppendLine($"Skills: {string.Join(", ", request.CandidateProfile.Skills)}");
            if (!string.IsNullOrWhiteSpace(request.CandidateProfile.Experience))
                sb.AppendLine($"Experience: {request.CandidateProfile.Experience}");
            if (!string.IsNullOrWhiteSpace(request.CandidateProfile.Education))
                sb.AppendLine($"Education: {request.CandidateProfile.Education}");
            sb.AppendLine("\nJOB REQUIREMENTS:");
            if (request.JobRequirements.RequiredSkills.Any())
                sb.AppendLine($"Required Skills: {string.Join(", ", request.JobRequirements.RequiredSkills)}");
            if (!string.IsNullOrWhiteSpace(request.JobRequirements.Experience))
                sb.AppendLine($"Required Experience: {request.JobRequirements.Experience}");
            if (!string.IsNullOrWhiteSpace(request.JobRequirements.Education))
                sb.AppendLine($"Required Education: {request.JobRequirements.Education}");
            sb.AppendLine("\nPlease provide: Match Score (0-100), Recommendation, Matched Skills (list), Missing Skills (list), and Match Details.");
            return sb.ToString();
        }

        // BuildInterviewQuestionsPrompt was removed here — interview questions
        // now generate through the same IAIServiceProvider gateway for every
        // provider (see GenerateInterviewQuestionsAsync), which prompts via
        // Digi.Core.AI.Providers.Generic.GenericPrompts instead.

        private async Task<(bool IsSuccess, string? Response, int TokensUsed, string? Error)> CallAIAPIAsync(
            string provider, string apiKey, string? endpoint, string model, string prompt, int maxTokens, decimal temperature)
        {
            try
            {
                endpoint ??= GetDefaultEndpoint(provider);
                _httpClient.DefaultRequestHeaders.Clear();

                switch (provider.ToLower())
                {
                    case "openai":
                        return await CallOpenAIAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                    case "anthropic":
                        return await CallAnthropicAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                    case "google":
                        return await CallGoogleAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                    case "multinetai":
                        return await CallMultinetAIAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                    default:
                        return (false, null, 0, "Unsupported provider");
                }
            }
            catch (Exception ex)
            {
                return (false, null, 0, ex.Message);
            }
        }

        /// <summary>
        /// Guard, not a worker — and deliberately so.
        ///
        /// This method sits on the generic prompt-in/text-out path shared by the
        /// OpenAI, Anthropic and Google providers. Multinet's in-house service is
        /// not shaped like that and cannot be served from here:
        ///
        ///  * There is no general-purpose completion endpoint. Each feature has
        ///    its own path (jobreq/generate, parser/extract, screening/screen …),
        ///    and by the time control reaches this method the feature has been
        ///    reduced to a prompt string — there is nothing left to route on.
        ///    Guessing a path is explicitly forbidden by the integration
        ///    contract, and the one guess already made (/api/query) 404s.
        ///  * Responses are structured JSON that maps onto specific screens, not
        ///    prose. Squeezing one back through a single string would discard the
        ///    field-level structure that makes the integration worth having.
        ///  * maxTokens and temperature are ignored by the service, which budgets
        ///    tokens per endpoint and pins temperature to 0 for deterministic
        ///    extraction. Honouring a 1000-token client cap would truncate a job
        ///    description that legitimately needs ~1700.
        ///
        /// Features backed by the AI service therefore branch to it BEFORE
        /// reaching here — see TestApiKeyViaGatewayAsync for the pattern. Anything
        /// that still arrives at this method is a feature the in-house service
        /// does not yet offer, and the honest answer is to say so.
        /// </summary>
        private Task<(bool IsSuccess, string? Response, int TokensUsed, string? Error)> CallMultinetAIAsync(
            string apiKey, string endpoint, string model, string prompt, int maxTokens, decimal temperature)
        {
            _logger.LogWarning(
                "A feature attempted to reach Multinet's AI service through the generic provider path. " +
                "That service has no free-form completion endpoint; the feature needs its own integration.");

            return Task.FromResult<(bool, string?, int, string?)>((
                false,
                null,
                0,
                "This feature is not available on Multinet's in-house AI service yet. The service exposes " +
                "purpose-built endpoints (job description generation, resume parsing, screening, candidate " +
                "ranking, interview questions and scoring) rather than a general-purpose text endpoint. " +
                "Either switch this company to a provider that accepts free-form prompts, or ask the AI " +
                "team to add an endpoint for this feature."));
        }

        private async Task<(bool IsSuccess, string? Response, int TokensUsed, string? Error)> CallOpenAIAsync(
            string apiKey, string endpoint, string model, string prompt, int maxTokens, decimal temperature)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = model,
                    messages = new[] { new { role = "user", content = prompt } },
                    max_tokens = maxTokens,
                    temperature = (double)temperature
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{endpoint}/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var text = jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                    var tokens = jsonDoc.RootElement.GetProperty("usage").GetProperty("total_tokens").GetInt32();
                    return (true, text, tokens, null);
                }
                else
                {
                    // Parse OpenAI error response for better error messages
                    var errorMessage = ParseOpenAIError(responseContent, response.StatusCode);
                    return (false, null, 0, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CallOpenAIAsync");
                return (false, null, 0, $"Error calling OpenAI API: {ex.Message}");
            }
        }

        private string ParseOpenAIError(string responseContent, System.Net.HttpStatusCode statusCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return $"HTTP {statusCode}: No response from OpenAI API";
                }

                var jsonDoc = JsonDocument.Parse(responseContent);
                if (jsonDoc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    var errorType = errorElement.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
                    var errorCode = errorElement.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : null;
                    var errorMessage = errorElement.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;

                    // Handle specific error types with user-friendly messages
                    if (errorCode == "insufficient_quota" || errorType == "insufficient_quota")
                    {
                        return "Your OpenAI API quota has been exceeded. Please check your billing and plan details at https://platform.openai.com/account/billing. You may need to upgrade your plan or add payment method.";
                    }

                    if (errorCode == "rate_limit_exceeded" || errorType == "rate_limit_exceeded")
                    {
                        return "OpenAI API rate limit exceeded. Please wait a few moments and try again. If this persists, consider upgrading your plan.";
                    }

                    if (errorCode == "invalid_api_key" || errorType == "invalid_request_error")
                    {
                        return $"Invalid OpenAI API key. Please verify your API key is correct and has not been revoked. {errorMessage}";
                    }

                    if (errorCode == "model_not_found" || errorCode == "invalid_model")
                    {
                        return $"The specified model '{errorMessage}' is not available. Please check your API key has access to this model or use a different model.";
                    }

                    // Return formatted error message
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        return $"OpenAI API Error ({errorCode ?? errorType ?? "Unknown"}): {errorMessage}";
                    }
                }

                // Fallback to raw response if parsing fails
                return $"HTTP {statusCode}: {responseContent}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing OpenAI error response");
                return $"HTTP {statusCode}: {responseContent}";
            }
        }

        private async Task<(bool IsSuccess, string? Response, int TokensUsed, string? Error)> CallAnthropicAsync(
            string apiKey, string endpoint, string model, string prompt, int maxTokens, decimal temperature)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var requestBody = new
                {
                    model = model,
                    max_tokens = maxTokens,
                    temperature = (double)temperature,
                    messages = new[] { new { role = "user", content = prompt } }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{endpoint}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var text = jsonDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
                    var tokens = jsonDoc.RootElement.GetProperty("usage").GetProperty("input_tokens").GetInt32() +
                                jsonDoc.RootElement.GetProperty("usage").GetProperty("output_tokens").GetInt32();
                    return (true, text, tokens, null);
                }
                else
                {
                    // Parse error response for better error messages
                    var errorMessage = ParseAnthropicError(responseContent, response.StatusCode);
                    return (false, null, 0, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CallAnthropicAsync");
                return (false, null, 0, $"Error calling Anthropic API: {ex.Message}");
            }
        }

        private string ParseAnthropicError(string responseContent, System.Net.HttpStatusCode statusCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return $"HTTP {statusCode}: No response from Anthropic API";
                }

                var jsonDoc = JsonDocument.Parse(responseContent);
                if (jsonDoc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    var errorType = errorElement.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
                    var errorMessage = errorElement.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;

                    if (errorType == "rate_limit_error")
                    {
                        return "Anthropic API rate limit exceeded. Please wait a few moments and try again.";
                    }

                    if (errorType == "authentication_error")
                    {
                        return $"Invalid Anthropic API key. Please verify your API key is correct. {errorMessage}";
                    }

                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        return $"Anthropic API Error ({errorType ?? "Unknown"}): {errorMessage}";
                    }
                }

                return $"HTTP {statusCode}: {responseContent}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Anthropic error response");
                return $"HTTP {statusCode}: {responseContent}";
            }
        }

        private async Task<(bool IsSuccess, string? Response, int TokensUsed, string? Error)> CallGoogleAsync(
            string apiKey, string endpoint, string model, string prompt, int maxTokens, decimal temperature)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } },
                    generationConfig = new
                    {
                        maxOutputTokens = maxTokens,
                        temperature = (double)temperature
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{endpoint}/{model}:generateContent?key={apiKey}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var text = jsonDoc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                    return (true, text, maxTokens, null); // Google doesn't always return token count
                }
                else
                {
                    // Parse error response for better error messages
                    var errorMessage = ParseGoogleError(responseContent, response.StatusCode);
                    return (false, null, 0, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CallGoogleAsync");
                return (false, null, 0, $"Error calling Google API: {ex.Message}");
            }
        }

        private string ParseGoogleError(string responseContent, System.Net.HttpStatusCode statusCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return $"HTTP {statusCode}: No response from Google API";
                }

                var jsonDoc = JsonDocument.Parse(responseContent);
                if (jsonDoc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    var errorCode = errorElement.TryGetProperty("code", out var codeElement) ? codeElement.GetInt32() : 0;
                    var errorMessage = errorElement.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;
                    var errorStatus = errorElement.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;

                    if (errorCode == 429 || errorStatus == "RESOURCE_EXHAUSTED")
                    {
                        return "Google API quota or rate limit exceeded. Please wait a few moments and try again, or check your quota limits.";
                    }

                    if (errorCode == 401 || errorStatus == "UNAUTHENTICATED")
                    {
                        return $"Invalid Google API key. Please verify your API key is correct. {errorMessage}";
                    }

                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        return $"Google API Error ({errorStatus ?? errorCode.ToString()}): {errorMessage}";
                    }
                }

                return $"HTTP {statusCode}: {responseContent}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Google error response");
                return $"HTTP {statusCode}: {responseContent}";
            }
        }

        // ParseScreeningResponse / ExtractListItems / ExtractExperienceMatch /
        // ExtractQualificationsMatch were removed here — screening responses now
        // deserialize directly into Digi.Core.AI.Contracts.ScreenCandidateResult
        // for every provider (see ScreenResumeAsync), so this regex-based
        // best-effort text scraping is no longer needed.

        private MatchCandidateResponseDto ParseMatchingResponse(string aiResponse, MatchCandidateRequestDto request)
        {
            var matchedSkills = request.CandidateProfile.Skills
                .Intersect(request.JobRequirements.RequiredSkills)
                .ToList();

            var missingSkills = request.JobRequirements.RequiredSkills
                .Except(request.CandidateProfile.Skills)
                .ToList();

            var matchScore = (int)((double)matchedSkills.Count / Math.Max(request.JobRequirements.RequiredSkills.Count, 1) * 100);

            return new MatchCandidateResponseDto
            {
                MatchScore = matchScore,
                MatchPercentage = matchScore,
                Recommendation = matchScore >= 80 ? "Strong Match" : matchScore >= 60 ? "Good Match" : "Weak Match",
                MatchedSkills = matchedSkills,
                MissingSkills = missingSkills,
                MatchDetails = aiResponse,
                MatchedOn = DateTime.UtcNow
            };
        }

        // ParseInterviewQuestionsResponse was removed here — responses now
        // deserialize directly into Digi.Core.AI.Contracts.InterviewQuestionsResult
        // for every provider, so this line-scraping heuristic (and its
        // fabricated placeholder questions when parsing came up short) is no
        // longer needed.

        public async Task<ApiResponse<ParseResumeResponseDto>> ParseResumeAsync(ParseResumeRequestDto request)
        {
            try
            {
                // Validation
                if (request == null)
                {
                    return ApiResponse<ParseResumeResponseDto>.Fail("Request body is required");
                }

                // Track processing time
                var startTime = DateTime.UtcNow;

                // Get API settings
                var settings = await _repository.GetApiKeySettingsAsync(request.CompanyId);
                if (settings == null)
                {
                    return ApiResponse<ParseResumeResponseDto>.Fail("API key settings not found. Please configure your AI provider settings first.");
                }

                // The "Auto Resume Parse" toggle gates the feature itself, not
                // just its automatic trigger — a recruiter's manual "Extract
                // with AI" button is still the AI doing the extraction, so it
                // is gated identically to the system-initiated call from the
                // careers apply form.
                if (settings.Settings?.AutoParse == false)
                {
                    return ApiResponse<ParseResumeResponseDto>.Fail(
                        "AI Resume Parsing is currently disabled. Please fill in the form below.");
                }

                var encryptedKey = await _repository.GetEncryptedApiKeyAsync(request.CompanyId);
                if (string.IsNullOrWhiteSpace(encryptedKey))
                {
                    return ApiResponse<ParseResumeResponseDto>.Fail("API key not found");
                }

                var apiKey = EncryptionHelper.DecryptText(encryptedKey);

                // Best-effort text for the audit trail column (parsedResumeText).
                // Every provider's actual extraction — bytes for Multinet, a local
                // text extract for the generic providers — happens inside
                // Digi.Core.AI itself; this copy is only for the DB record, so a
                // failure here must not fail the parse.
                string resumeText = string.Empty;
                try
                {
                    resumeText = await ExtractTextFromResumeFileAsync(request.ResumeFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not extract audit text from resume file: {FilePath}", request.ResumeFilePath);
                }

                // Every provider parses through the same IAIServiceProvider
                // gateway now — Multinet's purpose-built pipeline, or a
                // general-purpose model prompted for the identical
                // CandidateProfile shape (Digi.Core.AI.Providers.Generic).
                return await ParseResumeViaGatewayAsync(request, settings, apiKey, resumeText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing resume for CompanyID: {CompanyID}", request?.CompanyId ?? 0);
                return ApiResponse<ParseResumeResponseDto>.Fail($"Error parsing resume: {ex.Message}");
            }
        }


        /// <summary>
        /// True when a URL is one the AI service could realistically fetch.
        ///
        /// Loopback and private addresses are reachable from THIS process but
        /// not from the GPU host, and handing one over produces a failure that
        /// looks like a bad document rather than a bad address.
        /// </summary>
        private static bool IsRemotelyFetchable(string path)
        {
            if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            if (uri.IsLoopback)
            {
                return false;
            }

            var host = uri.Host;

            // RFC 1918 and link-local ranges, plus the ".local" mDNS suffix.
            if (host.StartsWith("10.", StringComparison.Ordinal) ||
                host.StartsWith("192.168.", StringComparison.Ordinal) ||
                host.StartsWith("169.254.", StringComparison.Ordinal) ||
                host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // 172.16.0.0 – 172.31.255.255
            if (host.StartsWith("172.", StringComparison.Ordinal) &&
                host.Split('.') is { Length: 4 } parts &&
                int.TryParse(parts[1], out var second) &&
                second >= 16 && second <= 31)
            {
                return false;
            }

            return true;
        }

        private async Task<ApiResponse<ParseResumeResponseDto>> ParseResumeViaGatewayAsync(
            ParseResumeRequestDto request,
            ApiKeySettingsResponseDto settings,
            string apiKey,
            string resumeText)
        {
            var resolution = ResolveProviderBaseUri(settings.Provider, settings.ApiEndpoint);
            if (!resolution.IsUsable)
            {
                return ApiResponse<ParseResumeResponseDto>.Fail(
                    resolution.Problem ?? "The AI service endpoint is not configured correctly.");
            }

            if (resolution.Warning is not null)
            {
                _logger.LogWarning(
                    "AI endpoint for CompanyID {CompanyID} needs attention: {Warning}",
                    request.CompanyId, resolution.Warning);
            }

            var startTime = DateTime.UtcNow;
            AiResult<ParseResumeResult> aiResult;

            var resolvedPath = ResolveLocalFilePath(request.ResumeFilePath);

            // A URL is only worth sending if the AI service can actually fetch
            // it. Ours runs on Multinet's GPU box, so a localhost or private
            // address is unreachable from there and the parse comes back as a
            // confusing "could not read the document" rather than a network
            // error. In that case fall through and upload the bytes instead,
            // which works from any environment.
            if (IsRemotelyFetchable(resolvedPath))
            {
                aiResult = await _aiResolver.Resolve(settings.Provider).ExtractResumeByUrlAsync(
                    documentUrl: resolvedPath,
                    candidateId: request.ApplicantID?.ToString(),
                    applicationId: request.ApplicationID?.ToString(),
                    companyId: request.CompanyId.ToString(),
                    apiKey: apiKey,
                    baseUriOverride: resolution.BaseUri,
                    model: settings.Model);
            }
            else
            {
                byte[]? fileBytes = null;
                string fileName = !string.IsNullOrWhiteSpace(request.ResumeFileName)
                    ? request.ResumeFileName
                    : Path.GetFileName(resolvedPath);

                try
                {
                    if (File.Exists(resolvedPath))
                    {
                        fileBytes = await File.ReadAllBytesAsync(resolvedPath);
                    }
                    else
                    {
                        var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", resolvedPath.TrimStart('/', '\\'));
                        if (File.Exists(wwwrootPath))
                        {
                            fileBytes = await File.ReadAllBytesAsync(wwwrootPath);
                        }
                        else
                        {
                            try
                            {
                                fileBytes = await _fileStorageService.GetFileAsync(resolvedPath);
                            }
                            catch (Exception storageEx)
                            {
                                _logger.LogWarning(storageEx, "Storage service could not read resume file for AI stream upload: {Path}", resolvedPath);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read resume file bytes for AI stream upload: {Path}", resolvedPath);
                }

                if (fileBytes != null && fileBytes.Length > 0)
                {
                    using var stream = new MemoryStream(fileBytes);
                    aiResult = await _aiResolver.Resolve(settings.Provider).ExtractResumeAsync(
                        stream,
                        fileName,
                        apiKey: apiKey,
                        baseUriOverride: resolution.BaseUri,
                        model: settings.Model);
                }
                else
                {
                    aiResult = await _aiResolver.Resolve(settings.Provider).ExtractResumeByUrlAsync(
                        documentUrl: resolvedPath,
                        candidateId: request.ApplicantID?.ToString(),
                        applicationId: request.ApplicationID?.ToString(),
                        companyId: request.CompanyId.ToString(),
                        apiKey: apiKey,
                        baseUriOverride: resolution.BaseUri,
                        model: settings.Model);
                }
            }

            if (aiResult.IsFailure)
            {
                var aiError = aiResult.Error!;
                _logger.LogWarning(
                    "Resume parsing via {Provider} failed for CompanyID {CompanyID}: {Code} (HTTP {Status}).",
                    settings.Provider, request.CompanyId, aiError.Code, aiError.HttpStatus);

                return ApiResponse<ParseResumeResponseDto>.Fail(aiError.Message);
            }

            var result = aiResult.Value!;
            if (result.Data == null)
            {
                return ApiResponse<ParseResumeResponseDto>.Fail($"{settings.Provider} returned success but profile data is empty");
            }

            var parsedData = MapCandidateProfileToDto(result.Data);
            parsedData.ParsedOn = DateTime.UtcNow;

            var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            var tokensUsed = (result.Meta?.PromptTokens ?? 0) + (result.Meta?.OutputTokens ?? 0);

            string parsedDataJson = System.Text.Json.JsonSerializer.Serialize(parsedData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            var fileType = request.FileType;
            if (string.IsNullOrWhiteSpace(fileType) && !string.IsNullOrWhiteSpace(request.ResumeFilePath))
            {
                var extension = Path.GetExtension(request.ResumeFilePath)?.TrimStart('.').ToUpper();
                fileType = extension ?? "UNKNOWN";
            }

            if (request.ApplicantID != 0)
            {
                var resumeFileName = request.ResumeFileName;
                if (string.IsNullOrWhiteSpace(resumeFileName) && !string.IsNullOrWhiteSpace(request.ResumeFilePath))
                {
                    resumeFileName = Path.GetFileName(request.ResumeFilePath);
                }

                var (parsingId, saveSuccess, saveMessage) = await _repository.SaveResumeParsingAsync(
                    companyId: request.CompanyId,
                    applicantId: request.ApplicantID,
                    applicationId: request.ApplicationID,
                    resumeFileName: resumeFileName,
                    resumeFilePath: request.ResumeFilePath,
                    fileType: fileType,
                    fileSize: request.FileSize,
                    parsedDataJson: parsedDataJson,
                    parsedResumeText: resumeText ?? string.Empty,
                    parsingMethod: "AI",
                    parsingProvider: settings.Provider ?? MultinetAiConstants.Name,
                    parsingModel: settings.Model ?? "qwen3.5:27b",
                    parsingStatus: "Success",
                    parsingConfidence: null,
                    parsingErrors: null,
                    tokensUsed: tokensUsed,
                    processingTime: processingTime,
                    userId: "System"
                );

                if (saveSuccess && parsingId.HasValue)
                {
                    _logger.LogInformation(
                        "✅ Resume parsing saved to database successfully. ParsingID: {ParsingID}, CompanyID: {CompanyID}",
                        parsingId.Value, request.CompanyId);
                }

                await _repository.SaveActivityAsync(
                    request.CompanyId,
                    "resume_parsing",
                    "Resume Parsed",
                    $"Parsed resume using {settings.Provider} ({settings.Model ?? "qwen3.5:27b"}). Processing time: {processingTime}ms. Parsing ID: {parsingId}",
                    parsingId
                );
            }

            return ApiResponse<ParseResumeResponseDto>.Success(parsedData, $"Resume parsed successfully using {settings.Provider}");
        }

        private static ParseResumeResponseDto MapCandidateProfileToDto(CandidateProfile profile)
        {
            return new ParseResumeResponseDto
            {
                FullName = profile.Name,
                Email = profile.Email,
                Phone = profile.Phone,
                Location = profile.Location,
                Summary = profile.Summary,
                Skills = profile.Skills ?? new List<string>(),
                Certifications = profile.CertificationsAndAwards ?? new List<string>(),
                Languages = profile.SpokenLanguages != null && profile.SpokenLanguages.Any()
                    ? string.Join(", ", profile.SpokenLanguages)
                    : null,
                Experience = profile.Experience?.Select(e => new ResumeExperienceDto
                {
                    Company = e.Company,
                    Role = e.Role,
                    Duration = e.Duration,
                    Location = e.Location,
                    Description = e.Achievements != null && e.Achievements.Any()
                        ? string.Join("; ", e.Achievements)
                        : null
                }).ToList() ?? new List<ResumeExperienceDto>(),
                Education = profile.Education?.Select(ed => new ResumeEducationDto
                {
                    Institution = ed.Institution,
                    Degree = ed.Degree,
                    Duration = ed.Duration,
                    Gpa = ed.Gpa
                }).ToList() ?? new List<ResumeEducationDto>(),
                Projects = profile.Projects?.Select(p => new ResumeProjectDto
                {
                    Name = p.Name,
                    Technologies = p.Technologies,
                    Description = p.Description != null && p.Description.Any()
                        ? string.Join("; ", p.Description)
                        : null
                }).ToList() ?? new List<ResumeProjectDto>(),
                TotalYearsExperience = EstimateTotalExperienceYears(profile.Experience, profile.Summary)
            };
        }

        /// <summary>
        /// Best-effort total years computation from experience duration strings and candidate summary text.
        /// Extracts explicit stated experience (e.g. "6+ years of experience") when experience node durations are missing or partial.
        /// Returns null when no usable dates or experience statements are found — never invents a number.
        /// </summary>
        private static int? EstimateTotalExperienceYears(List<ExperienceNode>? experiences, string? summary = null)
        {
            int? estimatedFromNodes = null;

            if (experiences != null && experiences.Any())
            {
                double totalMonths = 0;
                bool anyParsed = false;

                foreach (var exp in experiences)
                {
                    if (string.IsNullOrWhiteSpace(exp.Duration) ||
                        string.Equals(exp.Duration.Trim(), "Unknown", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var months = ParseDurationToMonths(exp.Duration);
                    if (months > 0)
                    {
                        totalMonths += months;
                        anyParsed = true;
                    }
                }

                if (anyParsed)
                {
                    var years = (int)Math.Round(totalMonths / 12.0);
                    estimatedFromNodes = years < 1 && totalMonths > 0 ? 1 : years;
                }
            }

            int? summaryYears = ExtractYearsFromText(summary);
            if (summaryYears.HasValue && summaryYears.Value > (estimatedFromNodes ?? 0))
            {
                return summaryYears.Value;
            }

            return estimatedFromNodes ?? summaryYears;
        }

        private static int? ExtractYearsFromText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var match = Regex.Match(text, @"(\d{1,2})\+?\s*years?\s*(?:of\s*)?exp", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var years))
            {
                return years;
            }
            return null;
        }

        /// <summary>
        /// Parses a duration string like "May 2026 - Present" or "Jul 2025 - Aug 2025"
        /// into approximate months. Returns 0 if unparseable.
        /// </summary>
        private static double ParseDurationToMonths(string duration)
        {
            // Common patterns: "Month Year - Month Year", "Month Year - Present"
            var parts = Regex.Split(duration, @"\s*[-–—to]+\s*", RegexOptions.IgnoreCase);
            if (parts.Length < 2) return 0;

            var startStr = parts[0].Trim();
            var endStr = parts[parts.Length - 1].Trim();

            if (!TryParseFlexibleDate(startStr, out var startDate))
                return 0;

            DateTime endDate;
            if (endStr.Equals("Present", StringComparison.OrdinalIgnoreCase) ||
                endStr.Equals("Current", StringComparison.OrdinalIgnoreCase) ||
                endStr.Equals("Now", StringComparison.OrdinalIgnoreCase))
            {
                endDate = DateTime.UtcNow;
            }
            else if (!TryParseFlexibleDate(endStr, out endDate))
            {
                return 0;
            }

            if (endDate < startDate) return 0;
            return (endDate.Year - startDate.Year) * 12 + (endDate.Month - startDate.Month);
        }

        private static bool TryParseFlexibleDate(string text, out DateTime result)
        {
            result = default;
            text = text.Trim().Replace(",", "");

            // Try standard DateTime parse first
            if (DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out result))
                return true;

            // Try "Month Year" pattern (e.g., "May 2026")
            var match = Regex.Match(text, @"^([A-Za-z]+)\s+(\d{4})$");
            if (match.Success)
            {
                var monthStr = match.Groups[1].Value;
                var yearStr = match.Groups[2].Value;
                if (DateTime.TryParseExact($"01 {monthStr} {yearStr}", "dd MMMM yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out result))
                    return true;
                if (DateTime.TryParseExact($"01 {monthStr} {yearStr}", "dd MMM yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out result))
                    return true;
            }

            // Try just a year "2025"
            var yearMatch = Regex.Match(text, @"^(\d{4})$");
            if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var year))
            {
                result = new DateTime(year, 1, 1);
                return true;
            }

            return false;
        }


        private static string ResolveLocalFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;

            path = path.Trim();

            if (path.StartsWith("~"))
            {
                var subPath = path.Substring(1).TrimStart('/', '\\');
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                if (subPath.StartsWith("Users/", StringComparison.OrdinalIgnoreCase))
                {
                    return "/" + subPath;
                }

                return Path.Combine(userProfile, subPath);
            }

            return path;
        }

        /// <summary>
        /// Extracts text from resume file (supports PDF, DOC, DOCX, TXT)
        /// </summary>
        private async Task<string> ExtractTextFromResumeFileAsync(string filePath)
        {
            try
            {
                filePath = ResolveLocalFilePath(filePath);

                // Get file bytes using FileStorageService
                byte[] fileBytes;
                string fullPath = filePath;

                // Check if it's an HTTP/HTTPS URL
                if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        fileBytes = await _httpClient.GetByteArrayAsync(filePath);
                        fullPath = filePath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to download resume file from URL: {URL}", filePath);
                        throw new FileNotFoundException($"Could not download resume from URL: {filePath}", ex);
                    }
                }
                // Check if it's a relative path (from storage)
                else if (!Path.IsPathRooted(filePath))
                {
                    try
                    {
                        fileBytes = await _fileStorageService.GetFileAsync(filePath);
                    }
                    catch (FileNotFoundException)
                    {
                        // Try absolute path
                        if (File.Exists(filePath))
                        {
                            fileBytes = await File.ReadAllBytesAsync(filePath);
                            fullPath = filePath;
                        }
                        else
                        {
                            // Try relative path from wwwroot/storage
                            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "storage");
                            fullPath = Path.Combine(uploadsRoot, filePath);
                            if (File.Exists(fullPath))
                            {
                                fileBytes = await File.ReadAllBytesAsync(fullPath);
                            }
                            else
                            {
                                throw new FileNotFoundException($"Resume file not found at path: {filePath}");
                            }
                        }
                    }
                }
                else
                {
                    if (File.Exists(filePath))
                    {
                        fileBytes = await File.ReadAllBytesAsync(filePath);
                    }
                    else
                    {
                        throw new FileNotFoundException($"Resume file not found at path: {filePath}");
                    }
                }

                // Get file extension
                var extension = Path.GetExtension(fullPath).ToLowerInvariant();
                
                // Extract text based on file type
                return extension switch
                {
                    ".txt" => ExtractTextFromTxt(fileBytes),
                    ".pdf" => await ExtractTextFromPdfAsync(fileBytes),
                    ".doc" or ".docx" => await ExtractTextFromWordAsync(fileBytes, extension),
                    _ => throw new NotSupportedException($"Unsupported file format: {extension}. Supported formats: .txt, .pdf, .doc, .docx")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from resume file: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Extracts text from TXT file
        /// </summary>
        private string ExtractTextFromTxt(byte[] fileBytes)
        {
            return Encoding.UTF8.GetString(fileBytes);
        }

        /// <summary>
        /// Extracts text from PDF file (basic implementation)
        /// Note: For production, consider using iText7 or PdfPig library
        /// </summary>
        private async Task<string> ExtractTextFromPdfAsync(byte[] fileBytes)
        {
            try
            {
                // Try using iText library if available
                // Check if iText.Kernel.Pdf is available
                var iTextKernelType = Type.GetType("iText.Kernel.Pdf.PdfDocument, iText.Kernel");
                if (iTextKernelType != null)
                {
                    return await ExtractTextFromPdfUsingITextAsync(fileBytes);
                }

                // Fallback: Basic text extraction (limited functionality)
                // This is a simple approach - for production, use proper PDF library
                _logger.LogWarning("iText library not found. Using basic PDF text extraction (limited).");
                
                // Basic approach: Try to find readable text in PDF bytes
                // This is a fallback and may not work for all PDFs
                var text = Encoding.UTF8.GetString(fileBytes);
                
                // Try to extract text between common PDF text markers
                var textMatches = Regex.Matches(text, @"\((.*?)\)", RegexOptions.Singleline);
                var extractedText = new StringBuilder();
                
                foreach (Match match in textMatches)
                {
                    var content = match.Groups[1].Value;
                    // Filter out non-text content
                    if (content.Length > 3 && content.Length < 200 && !content.Contains("\\"))
                    {
                        extractedText.AppendLine(content);
                    }
                }

                var result = extractedText.ToString();
                if (string.IsNullOrWhiteSpace(result))
                {
                    throw new InvalidOperationException("Could not extract text from PDF. Please ensure the PDF contains readable text (not scanned images).");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}. Please ensure the PDF contains readable text.", ex);
            }
        }

        /// <summary>
        /// Extracts text from PDF using iText library (if available)
        /// </summary>
        private async Task<string> ExtractTextFromPdfUsingITextAsync(byte[] fileBytes)
        {
            try
            {
                // Use reflection to call iText methods dynamically
                var pdfReaderType = Type.GetType("iText.Kernel.Pdf.PdfReader, iText.Kernel");
                var pdfDocumentType = Type.GetType("iText.Kernel.Pdf.PdfDocument, iText.Kernel");
                var pdfTextExtractorType = Type.GetType("iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor, iText.Kernel");

                if (pdfReaderType == null || pdfDocumentType == null || pdfTextExtractorType == null)
                {
                    throw new InvalidOperationException("iText library types not found");
                }

                using var stream = new MemoryStream(fileBytes);
                var pdfReader = Activator.CreateInstance(pdfReaderType, stream);
                var pdfDoc = Activator.CreateInstance(pdfDocumentType, pdfReader);

                var pageCount = (int)pdfDocumentType.GetProperty("NumberOfPages").GetValue(pdfDoc);
                var textBuilder = new StringBuilder();

                for (int i = 1; i <= pageCount; i++)
                {
                    var page = pdfDocumentType.GetMethod("GetPage").Invoke(pdfDoc, new object[] { i });
                    var extractTextMethod = pdfTextExtractorType.GetMethod("GetTextFromPage", new[] { page.GetType() });
                    var pageText = extractTextMethod.Invoke(null, new[] { page }) as string;
                    textBuilder.AppendLine(pageText);
                }

                pdfDoc.GetType().GetMethod("Close").Invoke(pdfDoc, null);
                pdfReader.GetType().GetMethod("Close").Invoke(pdfReader, null);

                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF using iText");
                throw;
            }
        }

        /// <summary>
        /// Extracts text from Word document (DOC/DOCX)
        /// Note: For production, consider using DocumentFormat.OpenXml or Aspose.Words
        /// </summary>
        private async Task<string> ExtractTextFromWordAsync(byte[] fileBytes, string extension)
        {
            try
            {
                if (extension == ".docx")
                {
                    // Try using DocumentFormat.OpenXml if available
                    var openXmlType = Type.GetType("DocumentFormat.OpenXml.Packaging.WordprocessingDocument, DocumentFormat.OpenXml");
                    if (openXmlType != null)
                    {
                        return await ExtractTextFromDocxUsingOpenXmlAsync(fileBytes);
                    }
                }

                // Fallback: Basic text extraction
                _logger.LogWarning("DocumentFormat.OpenXml library not found. Using basic Word text extraction (limited).");
                
                // Basic approach: Try to find readable text in Word file bytes
                var text = Encoding.UTF8.GetString(fileBytes);
                
                // Try to extract text between XML tags for DOCX
                if (extension == ".docx")
                {
                    var xmlMatches = Regex.Matches(text, @"<w:t[^>]*>([^<]*)</w:t>", RegexOptions.IgnoreCase);
                    var extractedText = new StringBuilder();
                    
                    foreach (Match match in xmlMatches)
                    {
                        extractedText.Append(match.Groups[1].Value + " ");
                    }

                    var result = extractedText.ToString();
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        return result;
                    }
                }

                // If extraction failed, throw exception
                throw new InvalidOperationException($"Could not extract text from {extension} file. Please ensure the file contains readable text.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from Word document");
                throw new InvalidOperationException($"Failed to extract text from Word document: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extracts text from DOCX using DocumentFormat.OpenXml (if available)
        /// </summary>
        private async Task<string> ExtractTextFromDocxUsingOpenXmlAsync(byte[] fileBytes)
        {
            try
            {
                var wordprocessingDocumentType = Type.GetType("DocumentFormat.OpenXml.Packaging.WordprocessingDocument, DocumentFormat.OpenXml");
                var openMethod = wordprocessingDocumentType.GetMethod("Open", new[] { typeof(Stream), typeof(bool) });
                
                using var stream = new MemoryStream(fileBytes);
                var wordDoc = openMethod.Invoke(null, new object[] { stream, false });
                
                var mainPartProperty = wordprocessingDocumentType.GetProperty("MainDocumentPart");
                var mainPart = mainPartProperty.GetValue(wordDoc);
                
                var documentProperty = mainPart.GetType().GetProperty("Document");
                var document = documentProperty.GetValue(mainPart);
                
                var bodyProperty = document.GetType().GetProperty("Body");
                var body = bodyProperty.GetValue(document);
                
                var innerTextProperty = body.GetType().GetProperty("InnerText");
                var text = innerTextProperty.GetValue(body) as string;
                
                wordDoc.GetType().GetMethod("Dispose").Invoke(wordDoc, null);
                
                return text ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from DOCX using OpenXml");
                throw;
            }
        }

        /// <summary>
        /// Cleans JSON response by removing markdown code blocks and extra whitespace
        /// </summary>
        private string CleanJsonResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return responseText;

            // Remove markdown code blocks
            responseText = Regex.Replace(
                responseText,
                @"```(?:json)?\s*",
                "",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            responseText = responseText.Replace("```", "").Trim();

            // Find JSON object boundaries
            var startIndex = responseText.IndexOf('{');
            var lastIndex = responseText.LastIndexOf('}');

            if (startIndex >= 0 && lastIndex > startIndex)
            {
                responseText = responseText.Substring(startIndex, lastIndex - startIndex + 1);
            }

            return responseText.Trim();
        }

        public async Task<ApiResponse<RankCandidatesResponseDto>> RankCandidatesAsync(RankCandidatesRequestDto request)
        {
            try
            {
                if (request == null || request.CandidateIds == null || !request.CandidateIds.Any())
                {
                    return ApiResponse<RankCandidatesResponseDto>.Fail("Candidate IDs are required");
                }

                // Track processing time
                var startTime = DateTime.UtcNow;

                var settings = await _repository.GetApiKeySettingsAsync(request.CompanyId);
                if (settings == null)
                {
                    return ApiResponse<RankCandidatesResponseDto>.Fail("API key settings not found");
                }

                // "Auto Candidate Matching" — no UI calls this endpoint today
                // (the AI team has confirmed /matching/rank returns an empty
                // list regardless, so it is deliberately not wired up yet —
                // see CLAUDE.md), but the setting should already be honoured
                // for the day it is.
                if (settings.Settings?.AutoMatching == false)
                {
                    return ApiResponse<RankCandidatesResponseDto>.Fail(
                        "Candidate matching is turned off for this company.");
                }

                // Get job requirements (you may need to fetch from database)
                var jobRequirements = "Job requirements will be fetched from database"; // TODO: Fetch actual requirements

                var prompt = $@"Rank the following candidates for a job position based on their resumes and the job requirements.

Job Requirements:
{jobRequirements}

Candidates to Rank: {string.Join(", ", request.CandidateIds)}

Return a JSON array of ranked candidates with the following structure:
[
    {{
        ""candidateId"": number,
        ""rank"": number,
        ""matchScore"": number (0-100),
        ""recommendation"": ""string"",
        ""strengths"": [""string""],
        ""weaknesses"": [""string""]
    }}
]";

                var encryptedKey = await _repository.GetEncryptedApiKeyAsync(request.CompanyId);
                if (string.IsNullOrWhiteSpace(encryptedKey))
                {
                    return ApiResponse<RankCandidatesResponseDto>.Fail("API key not found");
                }

                var apiKey = EncryptionHelper.DecryptText(encryptedKey);

                string? responseText = null;
                int tokensUsed = 0;

                var maxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 2000;
                var temperature = settings.Temperature > 0 ? settings.Temperature : 0.3m;
                var endpoint = settings.ApiEndpoint ?? GetDefaultEndpoint(settings.Provider);
                var model = settings.Model ?? (settings.Provider.ToLower() == "openai" ? "gpt-3.5-turbo" : settings.Provider.ToLower() == "anthropic" ? "claude-3-haiku-20240307" : "gemini-pro");

                switch (settings.Provider.ToLower())
                {
                    case "openai":
                        var openaiResult = await CallOpenAIAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                        responseText = openaiResult.Response;
                        tokensUsed = openaiResult.TokensUsed;
                        if (!openaiResult.IsSuccess)
                            return ApiResponse<RankCandidatesResponseDto>.Fail(openaiResult.Error ?? "Failed to call OpenAI API");
                        break;
                    case "anthropic":
                        var anthropicResult = await CallAnthropicAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                        responseText = anthropicResult.Response;
                        tokensUsed = anthropicResult.TokensUsed;
                        if (!anthropicResult.IsSuccess)
                            return ApiResponse<RankCandidatesResponseDto>.Fail(anthropicResult.Error ?? "Failed to call Anthropic API");
                        break;
                    case "google":
                        var googleResult = await CallGoogleAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                        responseText = googleResult.Response;
                        tokensUsed = googleResult.TokensUsed;
                        if (!googleResult.IsSuccess)
                            return ApiResponse<RankCandidatesResponseDto>.Fail(googleResult.Error ?? "Failed to call Google API");
                        break;
                    default:
                        return ApiResponse<RankCandidatesResponseDto>.Fail($"Unsupported AI provider: {settings.Provider}");
                }

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    return ApiResponse<RankCandidatesResponseDto>.Fail("Failed to rank candidates");
                }

                // Clean response text
                responseText = CleanJsonResponse(responseText);

                var rankedCandidates = System.Text.Json.JsonSerializer.Deserialize<List<RankedCandidateDto>>(
                    responseText, 
                    new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    });
                
                if (rankedCandidates == null || !rankedCandidates.Any())
                {
                    return ApiResponse<RankCandidatesResponseDto>.Fail("Failed to parse ranking response");
                }

                // Calculate processing time
                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                // Generate unique batch ID for this ranking session
                var rankingBatchId = $"RANK_{request.CompanyId}_{request.JobRequisitionId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";

                // Calculate percentile for each candidate (percentile = (total - rank + 1) / total * 100)
                var totalCandidates = rankedCandidates.Count;
                foreach (var candidate in rankedCandidates)
                {
                    candidate.Percentile = totalCandidates > 0 
                        ? Math.Round((decimal)(totalCandidates - candidate.Rank + 1) / totalCandidates * 100, 2)
                        : 0;
                }

                // Prepare ranking data for database
                var rankingDataList = new List<CandidateRankingData>();
                foreach (var candidate in rankedCandidates)
                {
                    // Serialize candidate ranking data to JSON
                    var rankingDataJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        candidateId = candidate.CandidateId,
                        rank = candidate.Rank,
                        matchScore = candidate.MatchScore,
                        recommendation = candidate.Recommendation,
                        strengths = candidate.Strengths,
                        weaknesses = candidate.Weaknesses,
                        percentile = candidate.Percentile
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = false });

                    rankingDataList.Add(new CandidateRankingData
                    {
                        ApplicationID = 0, // TODO: Fetch from database based on ApplicantID and RequisitionID
                        ApplicantID = candidate.CandidateId,
                        Rank = candidate.Rank,
                        OverallScore = candidate.MatchScore,
                        RankingDataJson = rankingDataJson,
                        Percentile = candidate.Percentile
                    });
                }

                // Save rankings to database
                var (rankingIds, saveSuccess, saveMessage) = await _repository.SaveCandidateRankingsAsync(
                    companyId: request.CompanyId,
                    requisitionId: request.JobRequisitionId,
                    rankings: rankingDataList,
                    rankingMethod: "AI",
                    rankingProvider: settings.Provider ?? "OpenAI",
                    rankingModel: model,
                    rankingBatchId: rankingBatchId,
                    totalCandidatesRanked: totalCandidates,
                    tokensUsed: tokensUsed,
                    processingTime: processingTime,
                    userId: "System" // TODO: Get from HttpContext or pass from controller
                );

                if (saveSuccess && rankingIds != null && rankingIds.Any())
                {
                    _logger.LogInformation(
                        "✅ Candidate rankings saved to database successfully. BatchID: {BatchID}, Total Rankings Saved: {Count}, RankingIDs: {RankingIDs}",
                        rankingBatchId, rankingIds.Count, string.Join(", ", rankingIds));
                }
                else
                {
                    _logger.LogWarning(
                        "⚠️ Failed to save candidate rankings to database: {Message}. BatchID: {BatchID}, Rankings Attempted: {Count}",
                        saveMessage, rankingBatchId, rankingDataList.Count);
                    // Continue even if save fails - return ranked data anyway
                }

                var response = new RankCandidatesResponseDto
                {
                    RankedCandidates = rankedCandidates,
                    RankedOn = DateTime.UtcNow
                };

                // Save activity
                await _repository.SaveActivityAsync(
                    request.CompanyId,
                    "candidate_ranking",
                    "Candidates Ranked",
                    $"Ranked {request.CandidateIds.Count} candidates for Requisition ID: {request.JobRequisitionId}. Batch ID: {rankingBatchId}",
                    request.JobRequisitionId
                );

                _logger.LogInformation(
                    "Candidates ranked successfully for CompanyID: {CompanyID}, RequisitionID: {RequisitionID}. Total: {Total}, Tokens: {Tokens}, ProcessingTime: {ProcessingTime}ms, BatchID: {BatchID}, Saved: {Saved}, RankingsSaved: {RankingsSaved}",
                    request.CompanyId, request.JobRequisitionId, totalCandidates, tokensUsed, processingTime, rankingBatchId, saveSuccess, rankingIds?.Count ?? 0);

                return ApiResponse<RankCandidatesResponseDto>.Success(response, "Candidates ranked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ranking candidates");
                return ApiResponse<RankCandidatesResponseDto>.Fail($"Error ranking candidates: {ex.Message}");
            }
        }

        public async Task<ApiResponse<GetInterviewScheduleSuggestionsResponseDto>> GetInterviewScheduleSuggestionsAsync(GetInterviewScheduleSuggestionsRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail("Request is required");
                }

                var settings = await _repository.GetApiKeySettingsAsync(request.CompanyId);
                if (settings == null)
                {
                    return ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail("API key settings not found");
                }

                var prompt = $@"Suggest optimal interview schedule slots for a candidate.

Job Requisition ID: {request.JobRequisitionId}
Candidate ID: {request.CandidateId}
Preferred Start Date: {request.PreferredStartDate?.ToString("yyyy-MM-dd") ?? "Not specified"}
Preferred End Date: {request.PreferredEndDate?.ToString("yyyy-MM-dd") ?? "Not specified"}
Interview Duration: {(request.InterviewDurationMinutes.HasValue ? request.InterviewDurationMinutes.Value : 60)} minutes
Interviewer IDs: {string.Join(", ", request.InterviewerIds ?? new List<int>())}

Suggest 5-10 optimal time slots considering:
- Interviewer availability
- Candidate timezone
- Business hours
- Buffer time between interviews

Return JSON array with structure:
[
    {{
        ""startTime"": ""ISO 8601 datetime"",
        ""endTime"": ""ISO 8601 datetime"",
        ""availableInterviewers"": [number],
        ""venue"": ""string"",
        ""priority"": number (1-5, 1=highest),
        ""reason"": ""string""
    }}
]";

                var encryptedKey = await _repository.GetEncryptedApiKeyAsync(request.CompanyId);
                if (string.IsNullOrWhiteSpace(encryptedKey))
                {
                    return ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail("API key not found");
                }

                var apiKey = EncryptionHelper.DecryptText(encryptedKey);

                string? responseText = null;
                int tokensUsed = 0;

                var maxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 2000;
                var temperature = settings.Temperature > 0 ? settings.Temperature : 0.3m;
                var endpoint = settings.ApiEndpoint ?? GetDefaultEndpoint(settings.Provider);
                var model = settings.Model ?? (settings.Provider.ToLower() == "openai" ? "gpt-3.5-turbo" : settings.Provider.ToLower() == "anthropic" ? "claude-3-haiku-20240307" : "gemini-pro");

                switch (settings.Provider.ToLower())
                {
                    case "openai":
                        var openaiResult = await CallOpenAIAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                        responseText = openaiResult.Response;
                        tokensUsed = openaiResult.TokensUsed;
                        if (!openaiResult.IsSuccess)
                            return ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail(openaiResult.Error ?? "Failed to call OpenAI API");
                        break;
                    case "anthropic":
                        var anthropicResult = await CallAnthropicAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                        responseText = anthropicResult.Response;
                        tokensUsed = anthropicResult.TokensUsed;
                        if (!anthropicResult.IsSuccess)
                            return ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail(anthropicResult.Error ?? "Failed to call Anthropic API");
                        break;
                    case "google":
                        var googleResult = await CallGoogleAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                        responseText = googleResult.Response;
                        tokensUsed = googleResult.TokensUsed;
                        if (!googleResult.IsSuccess)
                            return ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail(googleResult.Error ?? "Failed to call Google API");
                        break;
                    default:
                        return ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail($"Unsupported AI provider: {settings.Provider}");
                }

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    return ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail("Failed to generate interview schedule suggestions");
                }

                var suggestedSlots = System.Text.Json.JsonSerializer.Deserialize<List<InterviewSlotDto>>(responseText, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (suggestedSlots == null)
                {
                    return ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail("Failed to parse suggestions");
                }

                var response = new GetInterviewScheduleSuggestionsResponseDto
                {
                    SuggestedSlots = suggestedSlots,
                    GeneratedOn = DateTime.UtcNow
                };

                return ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Success(response, "Interview schedule suggestions generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating interview schedule suggestions");
                return ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail($"Error generating suggestions: {ex.Message}");
            }
        }

        public async Task<ApiResponse<GetSalaryRecommendationResponseDto>> GetSalaryRecommendationAsync(GetSalaryRecommendationRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return ApiResponse<GetSalaryRecommendationResponseDto>.Fail("Request is required");
                }

                var settings = await _repository.GetApiKeySettingsAsync(request.CompanyId);
                if (settings == null)
                {
                    return ApiResponse<GetSalaryRecommendationResponseDto>.Fail("API key settings not found");
                }

                var prompt = $@"Provide salary recommendation for a job position.

Job Requisition ID: {request.JobRequisitionId}
Job Title: {request.JobTitle ?? "Not specified"}
Location: {request.Location ?? "Not specified"}
Years of Experience: {request.YearsOfExperience?.ToString() ?? "Not specified"}
Skills: {string.Join(", ", request.Skills ?? new List<string>())}
Education Level: {request.EducationLevel ?? "Not specified"}

Consider:
- Market rates for similar positions
- Location cost of living
- Experience level
- Skills and qualifications
- Industry standards

Return JSON with structure:
{{
    ""recommendedMinSalary"": number,
    ""recommendedMaxSalary"": number,
    ""recommendedSalary"": number,
    ""currency"": ""string"",
    ""marketRange"": ""string"",
    ""factors"": [
        {{
            ""factor"": ""string"",
            ""impact"": ""string"",
            ""description"": ""string""
        }}
    ]
}}";

                var encryptedKey = await _repository.GetEncryptedApiKeyAsync(request.CompanyId);
                if (string.IsNullOrWhiteSpace(encryptedKey))
                {
                    return ApiResponse<GetSalaryRecommendationResponseDto>.Fail("API key not found");
                }

                var apiKey = EncryptionHelper.DecryptText(encryptedKey);

                string? responseText = null;
                int tokensUsed = 0;

                var maxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 2000;
                var temperature = settings.Temperature > 0 ? settings.Temperature : 0.3m;
                var endpoint = settings.ApiEndpoint ?? GetDefaultEndpoint(settings.Provider);
                var model = settings.Model ?? (settings.Provider.ToLower() == "openai" ? "gpt-3.5-turbo" : settings.Provider.ToLower() == "anthropic" ? "claude-3-haiku-20240307" : "gemini-pro");

                switch (settings.Provider.ToLower())
                {
                    case "openai":
                        var openaiResult = await CallOpenAIAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                        responseText = openaiResult.Response;
                        tokensUsed = openaiResult.TokensUsed;
                        if (!openaiResult.IsSuccess)
                            return ApiResponse<GetSalaryRecommendationResponseDto>.Fail(openaiResult.Error ?? "Failed to call OpenAI API");
                        break;
                    
                    case "anthropic":
                        var anthropicResult = await CallAnthropicAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                        responseText = anthropicResult.Response;
                        tokensUsed = anthropicResult.TokensUsed;
                        if (!anthropicResult.IsSuccess)
                            return ApiResponse<GetSalaryRecommendationResponseDto>.Fail(anthropicResult.Error ?? "Failed to call Anthropic API");
                        break;
                    case "google":
                        var googleResult = await CallGoogleAsync(apiKey, endpoint, model, prompt, maxTokens, temperature);
                        responseText = googleResult.Response;
                        tokensUsed = googleResult.TokensUsed;
                        if (!googleResult.IsSuccess)
                            return ApiResponse<GetSalaryRecommendationResponseDto>.Fail(googleResult.Error ?? "Failed to call Google API");
                        break;
                    default:
                        return ApiResponse<GetSalaryRecommendationResponseDto>.Fail($"Unsupported AI provider: {settings.Provider}");
                }

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    return ApiResponse<GetSalaryRecommendationResponseDto>.Fail("Failed to generate salary recommendation");
                }

                var recommendation = System.Text.Json.JsonSerializer.Deserialize<GetSalaryRecommendationResponseDto>(responseText, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (recommendation == null)
                {
                    return ApiResponse<GetSalaryRecommendationResponseDto>.Fail("Failed to parse recommendation");
                }

                recommendation.GeneratedOn = DateTime.UtcNow;

                // Save activity
                await _repository.SaveActivityAsync(
                    request.CompanyId,
                    "salary_recommendation",
                    "Salary Recommendation Generated",
                    $"Generated salary recommendation for Requisition ID: {request.JobRequisitionId}",
                    request.JobRequisitionId
                );

                return ApiResponse<GetSalaryRecommendationResponseDto>.Success(recommendation, "Salary recommendation generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating salary recommendation");
                return ApiResponse<GetSalaryRecommendationResponseDto>.Fail($"Error generating recommendation: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ParseResumeResponseDto>> ParseJobBankResumeAsync(ParseJobBankResumeRequestDto request)
        {
            try
            {
                var parseRequest = new ParseResumeRequestDto
                {
                    CompanyId = request.CompanyID,
                    ResumeFilePath = request.ResumePath,
                    ResumeFileName = request.ResumeFileName,
                    FileType = request.FileType,
                    FileSize = request.FileSize,
                    ApplicantID = 0,
                    ApplicationID = 0
                };

                var parseResult = await ParseResumeAsync(parseRequest);

                if (!parseResult.IsSuccess || parseResult.Data == null)
                    return parseResult;

                var parsed = (ParseResumeResponseDto)parseResult.Data;

                await _repository.UpdateJobBankCandidateFromParsedData(request.CompanyID,request.JobBankCandidateID,parsed);

                return ApiResponse<ParseResumeResponseDto>.Success(parsed, "Job bank resume parsed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing job bank resume");
                return ApiResponse<ParseResumeResponseDto>.Fail("Error parsing job bank resume");
            }
        }

        public async Task<ApiResponse<List<CandidateAIMatchDto>>> GetSavedAIMatchesAsync(int companyID,int jobRequisitionId)
        {
            try
            {
                var matches = await _repository.GetAIMatchesByRequisitionAsync(companyID,jobRequisitionId);
                return ApiResponse<List<CandidateAIMatchDto>>.Success(matches, "AI Matches fetched successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching AI matches for JobRequisitionID {JobRequisitionID}", jobRequisitionId);
                return ApiResponse<List<CandidateAIMatchDto>>.Fail("Failed to fetch AI matches");
            }
        }
    }
}
