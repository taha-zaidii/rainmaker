using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;

namespace Digi.Recruitment.Module.Domain.Services.IServices
{
    public interface IRecruitmentAIService
    {
        Task<ApiResponse<ApiKeyStatusResponseDto>> GetApiKeyStatusAsync(int companyId);
        Task<ApiResponse<ApiKeySettingsResponseDto>> GetApiKeySettingsAsync(int companyId);
        Task<ApiResponse<SaveApiKeySettingsResponseDto>> SaveApiKeySettingsAsync(SaveApiKeySettingsRequestDto request, string userId);
        Task<ApiResponse<TestApiKeyResponseDto>> TestApiKeyAsync(TestApiKeyRequestDto request);
        Task<ApiResponse<bool>> DeleteApiKeyAsync(int companyId);
        Task<ApiResponse<DashboardStatsResponseDto>> GetDashboardStatsAsync(int companyId);
        Task<ApiResponse<GenerateJobDescriptionResponseDto>> GenerateJobDescriptionAsync(GenerateJobDescriptionRequestDto request);
        Task<ApiResponse<SaveJobDescriptionResponseDto>> SaveJobDescriptionAsync(SaveJobDescriptionRequestDto request);
        Task<ApiResponse<ScreenResumeResponseDto>> ScreenResumeAsync(ScreenResumeRequestDto request);
        Task<ApiResponse<MatchCandidateResponseDto>> MatchCandidateAsync(MatchCandidateRequestDto request);
        Task<ApiResponse<GenerateInterviewQuestionsResponseDto>> GenerateInterviewQuestionsAsync(GenerateInterviewQuestionsRequestDto request);
        Task<ApiResponse<SaveSettingsResponseDto>> SaveSettingsAsync(SaveSettingsRequestDto request);
        Task<ApiResponse<GetSettingsResponseDto>> GetSettingsAsync(int companyId);
        Task<ApiResponse<ParseResumeResponseDto>> ParseResumeAsync(ParseResumeRequestDto request);
        Task<ApiResponse<RankCandidatesResponseDto>> RankCandidatesAsync(RankCandidatesRequestDto request);
        Task<ApiResponse<GetInterviewScheduleSuggestionsResponseDto>> GetInterviewScheduleSuggestionsAsync(GetInterviewScheduleSuggestionsRequestDto request);
        Task<ApiResponse<GetSalaryRecommendationResponseDto>> GetSalaryRecommendationAsync(GetSalaryRecommendationRequestDto request);
        Task<ApiResponse<ParseResumeResponseDto>> ParseJobBankResumeAsync(ParseJobBankResumeRequestDto request);
        Task<ApiResponse<List<CandidateAIMatchDto>>> GetSavedAIMatchesAsync(int companyID, int jobRequisitionId);
    }
}
