using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;

namespace Digi.Recruitment.Module.Domain.Repositories.IRepositories
{
    public interface IRecruitmentRepository
    {
        Task<EmployeeEmailDto> GetEmployeeEmailsAsync(int employeeIds);
        Task<IEnumerable<RecruitmentRequisitionPublicDto>> ManagePublicAsync(RecruitmentRequisitionActionDto dto);
        Task<int> UpdateApplicationStatusAsync(UpdateJobApplicationStatusDto dto);
        Task<RecruitmentJobDetailDto> GetJobDetailsAsync(int recruitmentRequisitionId);

        Task<IEnumerable<JobApplicationDto>> GetApplicationsByRequisitionAsync(int jobRequisitionId, int companyID);
        Task<IEnumerable<JobApplicationByShortListedDto>> GetApplicationsByShortListedAsync(int jobRequisitionId, int companyID);
        Task<(int? NewId, bool IsSuccess, string Message, RecruitmentRequisitionDto? Row)> SaveAsync(SaveRecruitmentRequisitionRequest request, CancellationToken cancellationToken = default);
        Task<RecruitmentRequisitionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<(bool IsSuccess, string Message, RecruitmentRequisitionDto? Row)> UpdateAsync(UpdateRecruitmentRequisitionRequest req, CancellationToken cancellationToken = default);
        Task<IEnumerable<RecruitmentRequisitionGetDto>> GetAllAsync(int? companyId, int? year, bool? isPublished, string? searchText, int pageNumber, int pageSize, string? employeeCode);
        Task<RecruitmentRequisitionGetDto> GetByIdAsync(int requisitionId);
        Task<IEnumerable<RecruitmentSummaryDto>> GetSummaryAsync(int? companyId, int? year);
        Task<IEnumerable<CandidateAssignGroupDto>> GetSummaryAsync(int? companyId, string? filterBy, int? year, string? searchText);

        Task<IEnumerable<ApplicationStatusDto>> GetInterviewStatusesAsync();

        // Auto Process Methods
        Task<(int ParsingID, bool IsSuccess, string Message)> AutoParseResumeAsync(AutoParseResumeRequestDto request, string createdBy);
        Task<(int ScreeningID, bool AutoShortlisted, bool IsSuccess, string Message)> AutoScreenResumeAsync(AutoScreenResumeRequestDto request, string createdBy);
        Task<(bool IsSuccess, string Message)> AutoShortlistCandidateAsync(AutoShortlistRequestDto request, string createdBy);

        // Interview Rounds Methods
        Task<List<InterviewRoundDto>> GetInterviewRoundsAsync(int companyID, int applicationID);
        Task<(int ScheduleID, string? ScheduleCode, bool IsSuccess, string Message)> ScheduleInterviewRoundAsync(ScheduleInterviewRoundRequestDto request, string createdBy);
        Task<(bool IsSuccess, string Message)> CompleteInterviewRoundAsync(CompleteInterviewRoundRequestDto request, string updatedBy);
        Task<GetApplicationsByInterviewStatusResponseDto> GetApplicationsByInterviewStatusAsync(GetApplicationsByInterviewStatusRequestDto request);

        // Helpers
        Task<ApplicationStatusUpdateDto?> GetApplicationStatusAsync(int applicationID);
        Task<IEnumerable<ApplicationStatusDto>> GetNotificationMethodAsync();
        Task<IEnumerable<ApplicationStatusDto>> GetVenueAsync();
        Task<IEnumerable<ApplicationStatusDto>> GetRecommendationAsync();
        Task<IEnumerable<ApplicationStatusDto>> GetOtherStageStatusesAsync();
        Task<IEnumerable<ShortlistedCandidateDto>> GetAllShortlistedAsync(int CompanyID);
      //  Task SaveInterviewScheduleAsync(InterviewScheduleRequestDto request);
        Task UpdateInterviewScheduleAsync(bool isHired, int jobApplicationID, int interviewStateID, int applicantID, int companyID, string empCode);
        Task<InterviewScheduleCollectionDto> GetAllInterviewSchedulesAsync(int CompanyID);
        Task<IEnumerable<CandidateEvaluationDto>> GetCandidateEvaluationsAsync(long? requisitionId, int? interviewRound);

        Task<ApiResponse<bool>> DeleteRecruitmentRequisitionAsync(int recruitmentRequisitionID, string employeeCode, string? reasonToDelete);

        // =============================================
        // CRUD OPERATIONS - APPLICANT
        // =============================================
        Task<(int ApplicantID, string ApplicantCode, bool IsSuccess, string Message)> CreateApplicantAsync(ApplicantCreateRequestDto request, string createdBy);
        Task<ApplicantResponseDto?> GetApplicantByIdAsync(int applicantID);
        Task<(List<ApplicantResponseDto> Applicants, int TotalCount)> GetAllApplicantsAsync(ApplicantListRequestDto request);
        Task<(bool IsSuccess, string Message)> UpdateApplicantAsync(ApplicantUpdateRequestDto request, string updatedBy);
        Task<(bool IsSuccess, string Message)> DeleteApplicantAsync(int applicantID, string deletedBy);

        // =============================================
        // CRUD OPERATIONS - JOB REQUISITION
        // =============================================
        Task<(int RequisitionID, string RequisitionCode, bool IsSuccess, string Message)> CreateJobRequisitionAsync(JobRequisitionCreateRequestDto request);
        Task<JobRequisitionResponseDto?> GetJobRequisitionByIdAsync(int requisitionID);
        Task<(bool IsSuccess, string Message)> UpsertHiringDetailAsync(int requisitionID, int companyID, JobRequisitionHiringDetailDto detail, string createdBy);
        Task<JobRequisitionHiringDetailDto?> GetHiringDetailByRequisitionIdAsync(int requisitionID, int? companyID = null);
        Task<(List<JobRequisitionResponseDto> Requisitions, int TotalCount)> GetAllJobRequisitionsAsync(JobRequisitionListRequestDto request);
        Task<(bool IsSuccess, string Message)> UpdateJobRequisitionAsync(JobRequisitionUpdateRequestDto request, string updatedBy);
        Task<(bool IsSuccess, string Message)> DeleteJobRequisitionAsync(int requisitionID, string deletedBy,int companyID);
        Task<(bool IsSuccess, string Message)> UpdateApplicationStatusOnlyAsync(int applicationId, int statusId, decimal? screeningScore, decimal? overallRating, string updatedBy);

        // =============================================
        // CRUD OPERATIONS - JOB APPLICATION
        // =============================================
        Task<(int ApplicationID, string ApplicationCode, bool IsSuccess, string Message)> CreateJobApplicationAsync(JobApplicationCreateRequestDto request, string createdBy);
        Task<JobApplicationResponseDto?> GetJobApplicationByIdAsync(int applicationID);
        Task<(List<JobApplicationResponseDto> Applications, int TotalCount)> GetAllJobApplicationsAsync(JobApplicationListRequestDto request);
        Task<(bool IsSuccess, string Message)> UpdateJobApplicationAsync(JobApplicationUpdateRequestDto request, string updatedBy);
        Task<(bool IsSuccess, string Message)> DeleteJobApplicationAsync(int applicationID, string deletedBy);

        // =============================================
        // CRUD OPERATIONS - INTERVIEW SCHEDULE
        // =============================================
        Task<(int ScheduleID, string ScheduleCode, bool IsSuccess, string Message)> CreateInterviewScheduleAsync(InterviewScheduleCreateRequestDto request, string createdBy);
        Task<InterviewScheduleResponseDto?> GetInterviewScheduleByIdAsync(int scheduleID);
        Task<(List<InterviewScheduleResponseDto> Schedules, int TotalCount)> GetAllInterviewSchedulesAsync(InterviewScheduleListRequestDto request);
        Task<(bool IsSuccess, string Message)> UpdateInterviewScheduleAsync(InterviewScheduleUpdateRequestDto request, string updatedBy);
        Task<(bool IsSuccess, string Message)> DeleteInterviewScheduleAsync(int scheduleID, string deletedBy);
        Task<bool> MarkInterviewAsNotifiedAsync(int scheduleId, string updatedBy);

        // =============================================
        // STATUS MANAGEMENT
        // =============================================
        Task<List<StatusResponseDto>> GetAllStatusesAsync(string? statusTypeCode = null, bool isActive = true);
        Task<List<StatusTypeResponseDto>> GetAllStatusTypesAsync(bool isActive = true);

        // =============================================
        // WORKFLOW OPERATIONS
        // =============================================
        
        // Manual Shortlist
        Task<(int NewStatusID, string NewStatusCode, bool IsSuccess, string Message)> ManualShortlistAsync(ManualShortlistRequestDto request, string updatedBy);
        
        // Assign Panel Members
        Task<(int PanelCount, bool IsSuccess, string Message)> AssignPanelMembersAsync(AssignPanelMembersRequestDto request, string createdBy);
        
        // Submit Evaluation
        Task<(int EvaluationID, bool IsSuccess, string Message)> SubmitEvaluationAsync(SubmitEvaluationRequestDto request, string createdBy);

        // Mark as Hired
        Task<(int NewStatusID, string NewStatusCode, bool IsSuccess, string Message)> MarkAsHiredAsync(MarkAsHiredRequestDto request, string updatedBy);

        // =============================================
        // MANUAL PROCESSING
        // =============================================
        Task<ManualProcessResponseDto> ManualProcessApplicationAsync(ManualProcessRequestDto request);
        Task<ManualParseResumeResponseDto> ManualParseResumeAsync(ManualParseResumeRequestDto request);
        Task<ManualScreenResumeResponseDto> ManualScreenResumeAsync(ManualScreenResumeRequestDto request);

        // =============================================
        // WORKFLOW ACTIONS
        // =============================================
        Task<ShortlistCandidateResponseDto> ShortlistCandidateAsync(int applicationID, ShortlistCandidateRequestDto request);
        Task<RejectApplicationResponseDto> RejectApplicationAsync(int applicationID, RejectApplicationRequestDto request);
        Task<HireCandidateResponseDto> HireCandidateAsync(int applicationID, HireCandidateRequestDto request);
        Task<PublishRequisitionResponseDto> PublishRequisitionAsync(int requisitionID, PublishRequisitionRequestDto request);
        Task<List<JobRequisitionResponseDto>> GetPublicRequisitionsAsync(int companyID, string? searchText, int? departmentID, string? location);
        Task<UpdateApplicationStatusResponseDto> UpdateApplicationStatusAsync(int applicationID, UpdateApplicationStatusRequestDto request);
        Task<CancelInterviewScheduleResponseDto> CancelInterviewScheduleAsync(int scheduleID, CancelInterviewScheduleRequestDto request);

        // =============================================
        // EVALUATION
        // =============================================

        Task<List<EvaluationCriteriaDto>> GetEvaluationCriteriaAsync(int companyID);
        Task<List<RatingScaleDto>> GetRatingScalesAsync(int companyID);
        Task<SubmitEvaluationResponseDto> SubmitEvaluationAsync(SubmitEvaluationRequestDto request);
        Task<List<EvaluationDto>> GetEvaluationsByScheduleAsync(int scheduleID);
        Task<List<EvaluationDto>> GetEvaluationsByApplicationAsync(int applicationID);

        // =============================================
        // MASTER DATA
        // =============================================
        Task<List<ApplicationSourceDto>> GetApplicationSourcesAsync(int companyID);
        Task<List<InterviewTypeDto>> GetInterviewTypesAsync(int companyID);
        Task<List<VenueDto>> GetVenuesAsync(int companyID);
        Task<List<NotificationMethodDto>> GetNotificationMethodsAsync(int companyID);

        // =============================================
        // STATUS MANAGEMENT
        // =============================================
        Task<List<StatusResponseDto>> GetStatusesByTypeAsync(string statusTypeCode, int companyID);

        // =============================================
        // DASHBOARD
        // =============================================
        Task<DashboardResponseDto> GetDashboardStatisticsAsync(int companyID);

        // =============================================
        // PANEL MEMBER EVALUATION
        // =============================================
        Task<PanelMemberScheduleListResponseDto> GetPanelMemberSchedulesAsync(int interviewerID, int companyID, int? statusID, DateTime? startDate, DateTime? endDate);
        Task<PanelEvaluationResponseDto?> GetPanelEvaluationAsync(int scheduleID, int interviewerID);
        Task<(int EvaluationID, bool IsSuccess, string Message)> SavePanelEvaluationAsync(PanelEvaluationRequestDto request);
        Task<(bool IsSuccess, string Message)> ConfirmPanelAttendanceAsync(int panelID, string confirmedBy);
        Task<List<RecommendationDto>> GetRecommendationsAsync(int companyID);
        Task<List<EvaluationCriteriaWithRatingsDto>> GetEvaluationCriteriaWithRatingsAsync(int companyID);

        Task<PanelMemberScheduleListResponseDto> GetConfirmedHeadSchedulesAsync(int companyID, int? statusID, DateTime? startDate, DateTime? endDate);
        Task<ApplicationAIStatusDto> GetApplicationAIStatusAsync(int applicationID, int companyID);

        /// <summary>Recruitment dashboard stats: Job Candidates, Job Applications Rejected, Total Jobs, Total Interviews (sp_Dashboard_RecStats).</summary>
        Task<List<RecDashboardRecStatsItemDto>> GetDashboardRecStatsAsync();

        // =============================================
        // JOB BANK
        // =============================================
        Task<(int JobBankCandidateID, bool IsSuccess, string Message)> JobBankCandidateInsertAsync(JobBankCandidateInsertRequestDto request);
        Task<(bool IsSuccess, string Message)> JobBankCandidateUpdateAsync(JobBankCandidateUpdateRequestDto request);
        Task<JobBankCandidateResponseDto?> JobBankCandidateGetByIdAsync(int jobBankCandidateID, int companyID);
        Task<(List<JobBankCandidateResponseDto> Candidates, int TotalRecords)> JobBankCandidateSearchAsync(JobBankCandidateSearchRequestDto request);
        Task<(List<JobBankCandidateResponseDto> Candidates, int TotalRecords)> JobBankCandidateGetListAsync(JobBankCandidateListRequestDto request);
        Task<(int JobBankShortlistID, bool IsSuccess, string Message)> JobBankShortlistInsertAsync(JobBankShortlistInsertRequestDto request);
        Task<List<JobBankShortlistByRequisitionDto>> JobBankShortlistGetByRequisitionAsync(int requisitionID, int companyID);
        Task<(bool IsSuccess, string Message)> JobBankShortlistRemoveAsync(int jobBankShortlistID, int companyID);

        Task<EvaluationResponseDtos?> GetEvaluationByScheduleAsync(int scheduleId);
        Task<bool> HireCandidateStatusAsync(int applicationID, HireCandidateDto dto);
        Task<(bool IsSuccess, string Message)> ConvertJobBankCandidateAsync(ConvertRequestDto request);
    }
}
