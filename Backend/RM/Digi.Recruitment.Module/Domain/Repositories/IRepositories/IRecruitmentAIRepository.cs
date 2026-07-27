using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;

namespace Digi.Recruitment.Module.Domain.Repositories.IRepositories
{
    public interface IRecruitmentAIRepository
    {
        Task<ApiKeyStatusResponseDto?> GetApiKeyStatusAsync(int companyId);
        Task<ApiKeySettingsResponseDto?> GetApiKeySettingsAsync(int companyId);
        Task<string?> GetEncryptedApiKeyAsync(int companyId); // Internal method to get actual encrypted API key
        Task<(int? Id, bool IsSuccess, string Message)> SaveApiKeySettingsAsync(SaveApiKeySettingsRequestDto request, string userId);
        Task<bool> DeleteApiKeyAsync(int companyId);
        Task<DashboardStatsResponseDto?> GetDashboardStatsAsync(int companyId);
        Task<(int? Id, bool IsSuccess, string Message)> SaveJobDescriptionAsync(int companyId, int? jobRequisitionId, string generatedDescription, string promptUsed, string model, int tokensUsed, string userId);
        Task<(int? Id, bool IsUpdate, bool IsSuccess, string Message, int? JobRequisitionId)> SaveJobDescriptionWithUpdateAsync(SaveJobDescriptionRequestDto request, string userId);
        Task<(int? Id, bool IsSuccess, string Message)> SaveResumeScreeningAsync(
            int companyId, 
            int? applicationId, 
            int? applicantId, 
            int? resumeParsingId, 
            int matchScore, 
            string skillsMatch, 
            string experienceMatch, 
            string qualificationsMatch, 
            string redFlags, 
            string recommendation, 
            string screeningMethod, 
            string screeningProvider, 
            string modelUsed, 
            int processingTime, 
            string userId);
        Task<(int? Id, bool IsSuccess, string Message)> SaveActivityAsync(int companyId, string activityType, string title, string description, int? relatedId);
        Task<GetSettingsResponseDto?> GetSettingsAsync(int companyId);
        Task<(bool IsSuccess, string Message)> SaveSettingsAsync(SaveSettingsRequestDto request);
        Task<(int? Id, bool IsSuccess, string Message)> SaveResumeParsingAsync(
            int companyId,
            int? applicantId,
            int? applicationId,
            string? resumeFileName,
            string resumeFilePath,
            string? fileType,
            long? fileSize,
            string parsedDataJson,
            string parsedResumeText,
            string parsingMethod,
            string parsingProvider,
            string parsingModel,
            string parsingStatus,
            decimal? parsingConfidence,
            string? parsingErrors,
            int tokensUsed,
            int processingTime,
            string userId);
        Task<(List<int> RankingIds, bool IsSuccess, string Message)> SaveCandidateRankingsAsync(
            int companyId,
            int requisitionId,
            List<CandidateRankingData> rankings,
            string rankingMethod,
            string rankingProvider,
            string rankingModel,
            string rankingBatchId,
            int totalCandidatesRanked,
            int tokensUsed,
            int processingTime,
            string userId);
        Task UpdateJobBankCandidateFromParsedData(int companyId, int candidateId, ParseResumeResponseDto parsed);

        Task SaveCandidateAIMatchAsync(int companyId, int requisitionId, int candidateId, MatchCandidateResponseDto result, string createdBy);
        Task<List<CandidateAIMatchDto>> GetAIMatchesByRequisitionAsync(int companyID, int jobRequisitionId);
    }

    public class CandidateRankingData
    {
        public int ApplicationID { get; set; }
        public int ApplicantID { get; set; }
        public int Rank { get; set; }
        public decimal OverallScore { get; set; }
        public string RankingDataJson { get; set; } = string.Empty;
        public decimal Percentile { get; set; }
    }
}
