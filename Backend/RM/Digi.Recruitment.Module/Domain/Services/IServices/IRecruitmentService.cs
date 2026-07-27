using Digi.Shared.DTOs;
using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;


namespace Digi.Recruitment.Module.Domain.Services.IServices
{
    public interface IRecruitmentService
    {
        //Task<ApiResponse<IEnumerable<RecruitmentRequisitionPublicDto>>> ManagePublicAsync(RecruitmentRequisitionActionDto dto);

        //Task<ApiResponse<bool>> UpdateApplicationStatusAsync(UpdateJobApplicationStatusDto dto);
        //Task<ApiResponse<RecruitmentJobDetailDto>> GetJobDetailsAsync(int recruitmentRequisitionId);

        //Task<ApiResponse<IEnumerable<JobApplicationDto>>> GetApplicationsByRequisitionAsync(int jobRequisitionId, int companyID);

        //Task<ApiResponse<IEnumerable<JobApplicationByShortListedDto>>> GetApplicationsByShortListedAsync(int jobRequisitionId, int companyID);

        //Task<ApiResponse<RecruitmentRequisitionDto?>> SaveAsync(SaveRecruitmentRequisitionRequest request, int? employeeID,CancellationToken cancellationToken = default);
        //Task<ApiResponse<RecruitmentRequisitionDto?>> GetByIdAsync(int id, CancellationToken cancellationToken = default); // extra method to match the repository interface
        //Task<ApiResponse<RecruitmentRequisitionDto?>> UpdateRecruitmentAsync(UpdateRecruitmentRequisitionRequest request,CancellationToken cancellationToken = default);

        //Task<ApiResponse<IEnumerable<RecruitmentRequisitionGetDto>>> GetAllAsync(int? companyId, int? year, bool? isPublished, string? searchText, int pageNumber, int pageSize, string? employeeCode);
        //Task<ApiResponse<RecruitmentRequisitionGetDto>> GetByIdAsync(int requisitionId);
        //Task<ApiResponse<IEnumerable<RecruitmentSummaryDto>>> GetSummaryAsync(int? companyId, int? year);


        //Task<ApiResponse<IEnumerable<CandidateAssignGroupDto>>> GetSummaryAsync(int? companyId, string? filterBy, int? year, string? searchText);

        //Task<ApiResponse<IEnumerable<ApplicationStatusDto>>> GetInterviewStatusesAsync();
        //Task<ApiResponse<IEnumerable<ApplicationStatusDto>>> GetRecommendationAsync();
        //Task<ApiResponse<IEnumerable<ApplicationStatusDto>>> GetVenueAsync();
        //Task<ApiResponse<IEnumerable<ApplicationStatusDto>>> GetNotificationMethodAsync();
        //Task<ApiResponse<IEnumerable<ApplicationStatusDto>>> GetOtherStageStatusesAsync();
        //Task<ApiResponse<IEnumerable<ShortlistedCandidateDto>>> GetAllShortlistedAsync(int CompanyID);
        //Task<ApiResponse<bool>> SaveInterviewScheduleAsync(InterviewScheduleRequestDto request);

       
        //Task<ApiResponse<bool>> UpdateInterviewScheduleAsync(bool isHired, int jobApplicationID, int interviewStateID, int applicantID, int companyID, string empCode);
        //Task<ApiResponse<InterviewScheduleCollectionDto>> GetAllInterviewSchedulesAsync(int CompanyID);
        //Task<ApiResponse<IEnumerable<CandidateEvaluationDto>>> GetCandidateEvaluationsAsync(long? requisitionId, int? interviewRound);
        //Task<ApiResponse<IEnumerable<SchedulePanelAssignListDto>>> GetAssignListAsync(int? scheduleHeaderId,int CompanyID);

        //Task<ApiResponse<IEnumerable<ScheduleAssignInterviewListDto>>> GetAssignListJobReqAsync(int companyID, int? interviewerID);

        //Task<EmployeeEmailDto> GetEmployeeEmailsAsync(int employeeIds);
        //Task<ApiResponse<bool>> DeleteRecruitmentRequisitionAsync(int recruitmentRequisitionID, string employeeCode, string? reasonToDelete);

        // Auto Process Methods
        Task<ApiResponse<AutoProcessResponseDto>> AutoProcessApplicationAsync(AutoProcessRequestDto request);
        Task<ApiResponse<AutoParseResumeResponseDto>> AutoParseResumeAsync(AutoParseResumeRequestDto request);
        Task<ApiResponse<AutoScreenResumeResponseDto>> AutoScreenResumeAsync(AutoScreenResumeRequestDto request);
        Task<ApiResponse<AutoShortlistResponseDto>> AutoShortlistCandidateAsync(AutoShortlistRequestDto request);

        // Interview Rounds Methods
        Task<ApiResponse<GetInterviewRoundsResponseDto>> GetInterviewRoundsAsync(int companyID, int applicationID);
        Task<ApiResponse<ScheduleInterviewRoundResponseDto>> ScheduleInterviewRoundAsync(ScheduleInterviewRoundRequestDto request);
        Task<ApiResponse<CompleteInterviewRoundResponseDto>> CompleteInterviewRoundAsync(CompleteInterviewRoundRequestDto request);
        Task<ApiResponse<GetApplicationsByInterviewStatusResponseDto>> GetApplicationsByInterviewStatusAsync(GetApplicationsByInterviewStatusRequestDto request);

        // =============================================
        // CRUD OPERATIONS - APPLICANT
        // =============================================
        Task<ApiResponse<ApplicantResponseDto>> CreateApplicantAsync(ApplicantCreateRequestDto request, string createdBy);
        Task<ApiResponse<ApplicantResponseDto>> GetApplicantByIdAsync(int applicantID);
        Task<ApiResponse<ApplicantListResponseDto>> GetAllApplicantsAsync(ApplicantListRequestDto request);
        Task<ApiResponse<bool>> UpdateApplicantAsync(ApplicantUpdateRequestDto request, string updatedBy);
        Task<ApiResponse<bool>> DeleteApplicantAsync(int applicantID, string deletedBy);

        // =============================================
        // CRUD OPERATIONS - JOB REQUISITION
        // =============================================
        Task<ApiResponse<JobRequisitionResponseDto>> CreateJobRequisitionAsync(JobRequisitionCreateRequestDto request);
        Task<ApiResponse<JobRequisitionResponseDto>> GetJobRequisitionByIdAsync(int requisitionID);
        Task<ApiResponse<JobRequisitionListResponseDto>> GetAllJobRequisitionsAsync(JobRequisitionListRequestDto request);
        Task<ApiResponse<bool>> UpdateJobRequisitionAsync(JobRequisitionUpdateRequestDto request, string updatedBy);
        Task<ApiResponse<bool>> DeleteJobRequisitionAsync(int requisitionID, string deletedBy,int companyID);

        // =============================================
        // CRUD OPERATIONS - JOB APPLICATION
        // =============================================
        Task<ApiResponse<JobApplicationResponseDto>> CreateJobApplicationAsync(JobApplicationCreateRequestDto request, string createdBy);
        Task<ApiResponse<JobApplicationResponseDto>> GetJobApplicationByIdAsync(int applicationID);
        Task<ApiResponse<JobApplicationListResponseDto>> GetAllJobApplicationsAsync(JobApplicationListRequestDto request);
        Task<ApiResponse<bool>> UpdateJobApplicationAsync(JobApplicationUpdateRequestDto request);
        Task<ApiResponse<bool>> DeleteJobApplicationAsync(int applicationID);

        // =============================================
        // CRUD OPERATIONS - INTERVIEW SCHEDULE
        // =============================================
        Task<ApiResponse<InterviewScheduleResponseDto>> CreateInterviewScheduleAsync(InterviewScheduleCreateRequestDto request, string createdBy, int companyID);
        Task<ApiResponse<InterviewScheduleResponseDto>> GetInterviewScheduleByIdAsync(int scheduleID);
        Task<ApiResponse<InterviewScheduleListResponseDto>> GetAllInterviewSchedulesAsync(InterviewScheduleListRequestDto request);
        Task<ApiResponse<bool>> UpdateInterviewScheduleAsync(InterviewScheduleUpdateRequestDto request);
        Task<ApiResponse<bool>> DeleteInterviewScheduleAsync(int scheduleID);

        // =============================================
        // STATUS MANAGEMENT
        // =============================================
        Task<ApiResponse<List<StatusResponseDto>>> GetAllStatusesAsync(string? statusTypeCode = null, bool isActive = true);
        Task<ApiResponse<List<StatusTypeResponseDto>>> GetAllStatusTypesAsync(bool isActive = true);
        Task<ApiResponse<List<StatusResponseDto>>> GetStatusesByTypeAsync(string statusTypeCode, int companyID);

        // =============================================
        // MANUAL PROCESSING
        // =============================================
        Task<ApiResponse<ManualProcessResponseDto>> ManualProcessApplicationAsync(ManualProcessRequestDto request);
        Task<ApiResponse<ManualParseResumeResponseDto>> ManualParseResumeAsync(ManualParseResumeRequestDto request);
        Task<ApiResponse<ManualScreenResumeResponseDto>> ManualScreenResumeAsync(ManualScreenResumeRequestDto request);

        // =============================================
        // WORKFLOW ACTIONS
        // =============================================
        Task<ApiResponse<ManualShortlistResponseDto>> ManualShortlistAsync(ManualShortlistRequestDto request);
        Task<ApiResponse<ShortlistCandidateResponseDto>> ShortlistCandidateAsync(int applicationID, ShortlistCandidateRequestDto request);
        Task<ApiResponse<RejectApplicationResponseDto>> RejectApplicationAsync(int applicationID, RejectApplicationRequestDto request);
        Task<ApiResponse<HireCandidateResponseDto>> HireCandidateAsync(int applicationID, HireCandidateRequestDto request);
        Task<ApiResponse<PublishRequisitionResponseDto>> PublishRequisitionAsync(int requisitionID, PublishRequisitionRequestDto request);
        Task<ApiResponse<List<JobRequisitionResponseDto>>> GetPublicRequisitionsAsync(int companyID, string? searchText, int? departmentID, string? location);
        Task<ApiResponse<UpdateApplicationStatusResponseDto>> UpdateApplicationStatusAsync(int applicationID, UpdateApplicationStatusRequestDto request);
        Task<ApiResponse<CancelInterviewScheduleResponseDto>> CancelInterviewScheduleAsync(int scheduleID, CancelInterviewScheduleRequestDto request);

        // =============================================
        // EVALUATION
        // =============================================
        Task<ApiResponse<List<EvaluationCriteriaDto>>> GetEvaluationCriteriaAsync(int companyID);
        Task<ApiResponse<List<RatingScaleDto>>> GetRatingScalesAsync(int companyID);
        Task<ApiResponse<SubmitEvaluationResponseDto>> SubmitEvaluationAsync(SubmitEvaluationRequestDto request);
        Task<ApiResponse<List<EvaluationDto>>> GetEvaluationsByScheduleAsync(int scheduleID);
        Task<ApiResponse<List<EvaluationDto>>> GetEvaluationsByApplicationAsync(int applicationID);

        // =============================================
        // MASTER DATA
        // =============================================
        Task<ApiResponse<List<ApplicationSourceDto>>> GetApplicationSourcesAsync(int companyID);
        Task<ApiResponse<List<InterviewTypeDto>>> GetInterviewTypesAsync(int companyID);
        Task<ApiResponse<List<VenueDto>>> GetVenuesAsync(int companyID);
        Task<ApiResponse<List<NotificationMethodDto>>> GetNotificationMethodsAsync(int companyID);

        // =============================================
        // DASHBOARD
        // =============================================
        Task<ApiResponse<DashboardResponseDto>> GetDashboardStatisticsAsync(int companyID);

        //Approval
        Task<DbOperationResult<bool>> SubmitJobRequisitionForApprovalAsync(int requisitionId, int companyId, int employeeId, string actor);

        // =============================================
        // PANEL MEMBER EVALUATION
        // =============================================
        Task<ApiResponse<PanelMemberScheduleListResponseDto>> GetPanelMemberSchedulesAsync(int interviewerID, int companyID, int? statusID, DateTime? startDate, DateTime? endDate);
        Task<ApiResponse<PanelEvaluationResponseDto>> GetPanelEvaluationAsync(int scheduleID, int interviewerID);
        Task<ApiResponse<PanelEvaluationResponseDto>> SavePanelEvaluationAsync(PanelEvaluationRequestDto request);
        Task<ApiResponse<ConfirmPanelAttendanceResponseDto>> ConfirmPanelAttendanceAsync(int panelID, ConfirmPanelAttendanceRequestDto request);
        Task<ApiResponse<List<RecommendationDto>>> GetRecommendationsAsync(int companyID);
        Task<ApiResponse<List<EvaluationCriteriaWithRatingsDto>>> GetEvaluationCriteriaWithRatingsAsync(int companyID);

        Task<ApiResponse<PanelMemberScheduleListResponseDto>> GetConfirmedHeadSchedulesAsync(int companyID, int? statusID, DateTime? startDate, DateTime? endDate);
        Task<ApiResponse<ApplicationAIStatusDto>> GetApplicationAIStatusAsync(int applicationID, int companyID);

        /// <summary>Recruitment dashboard stats (Job Candidates, Rejected, Total Jobs, Total Interviews).</summary>
        Task<ApiResponse<RecDashboardRecStatsResponseDto>> GetDashboardRecStatsAsync();

        // =============================================
        // JOB BANK
        // =============================================
        Task<ApiResponse<JobBankCandidateInsertResponseDto>> JobBankCandidateInsertAsync(JobBankCandidateInsertRequestDto request, string? createdBy);
        Task<ApiResponse<bool>> JobBankCandidateUpdateAsync(JobBankCandidateUpdateRequestDto request);
        Task<ApiResponse<JobBankCandidateResponseDto>> JobBankCandidateGetByIdAsync(int id, int companyID);
        Task<ApiResponse<JobBankCandidateSearchResponseDto>> JobBankCandidateSearchAsync(JobBankCandidateSearchRequestDto request);
        Task<ApiResponse<JobBankCandidateSearchResponseDto>> JobBankCandidateGetListAsync(JobBankCandidateListRequestDto request);
        Task<ApiResponse<JobBankShortlistInsertResponseDto>> JobBankShortlistInsertAsync(JobBankShortlistInsertRequestDto request);
        Task<ApiResponse<List<JobBankShortlistByRequisitionDto>>> JobBankShortlistGetByRequisitionAsync(int requisitionID, int companyID);
        Task<ApiResponse<bool>> JobBankShortlistRemoveAsync(int jobBankShortlistID, int companyID);
        Task<DbOperationResult<bool>> CloseJobRequisitionAsync(int requisitionId, int companyId, string actor);
        Task<DbOperationResult<bool>> SendInterviewNotificationAsync(int scheduleId, int companyId, string actor, string? formUrl);
        Task<ApiResponse<EvaluationResponseDtos>> GetEvaluationByScheduleAsync(int scheduleId);
        Task<bool> HireCandidateStatus(int applicationID, HireCandidateDto dto);
        Task<ApiResponse<object>> ConvertJobBankCandidateAsync(ConvertRequestDto request);
    }
}
