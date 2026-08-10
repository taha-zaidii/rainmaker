using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.hrm.module
{
    // =============================================
    // MANUAL PROCESSING DTOs
    // =============================================

    public class ManualProcessRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        [Required]
        public int ApplicationID { get; set; }
        [Required]
        public int ApplicantID { get; set; }
        [Required]
        public int RequisitionID { get; set; }
        public string? ResumePath { get; set; }
        public string? ResumeFileName { get; set; }
        public bool EnableManualParsing { get; set; } = true;
        public bool EnableManualScreening { get; set; } = true;
        public decimal? ManualScreeningScore { get; set; }
        public string? ManualRecommendation { get; set; }
        public object? ParsedData { get; set; }
        public string? ProcessedBy { get; set; }
    }

    public class ManualProcessResponseDto
    {
        public int ApplicationID { get; set; }
        public bool ResumeParsed { get; set; }
        public int? ResumeParsingID { get; set; }
        public object? ParsedData { get; set; }
        public bool ManuallyScreened { get; set; }
        public int? ScreeningID { get; set; }
        public decimal? ManualScreeningScore { get; set; }
        public decimal? MatchScore { get; set; }
        public string? Recommendation { get; set; }
        public string ProcessingMethod { get; set; } = "MANUAL";
        public string? ProcessedBy { get; set; }
        public DateTime ProcessedOn { get; set; }
    }

    public class ManualParseResumeRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        [Required]
        public int ApplicationID { get; set; }
        [Required]
        public int ApplicantID { get; set; }
        public string? ResumePath { get; set; }
        public string? ResumeFileName { get; set; }
        [Required]
        public object ParsedData { get; set; } = new();
        public string? ParsedBy { get; set; }
    }

    public class ManualParseResumeResponseDto
    {
        public int ParsingID { get; set; }
        public object? ParsedData { get; set; }
        public string ParsingMethod { get; set; } = "MANUAL";
        public string? ParsedBy { get; set; }
        public DateTime ParsedOn { get; set; }
    }

    public class ManualScreenResumeRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        [Required]
        public int ApplicationID { get; set; }
        [Required]
        public int ApplicantID { get; set; }
        [Required]
        public int RequisitionID { get; set; }
        public int? ResumeParsingID { get; set; }
        [Required]
        [Range(0, 100)]
        public decimal MatchScore { get; set; }
        [Required]
        [StringLength(50)]
        public string Recommendation { get; set; } = string.Empty;
        public object? SkillsMatch { get; set; }
        public object? ExperienceMatch { get; set; }
        public object? QualificationsMatch { get; set; }
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<string> RedFlags { get; set; } = new();
        public string? ScreenedBy { get; set; }
        public string? ScreeningNotes { get; set; }
    }

    public class ManualScreenResumeResponseDto
    {
        public int ScreeningID { get; set; }
        public decimal MatchScore { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public object? SkillsMatch { get; set; }
        public object? ExperienceMatch { get; set; }
        public object? QualificationsMatch { get; set; }
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public string ScreeningMethod { get; set; } = "MANUAL";
        public string? ScreenedBy { get; set; }
        public DateTime ScreenedOn { get; set; }
    }

    // =============================================
    // EVALUATION DTOs
    // =============================================

    public class EvaluationCriteriaDto
    {
        public int CriteriaID { get; set; }
        public string CriteriaCode { get; set; } = string.Empty;
        public string CriteriaTitle { get; set; } = string.Empty;
        public string? CriteriaType { get; set; }
        public string? Description { get; set; }
        public int MaxScore { get; set; }
        public decimal? Weightage { get; set; }
        public bool IsActive { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class RatingScaleDto
    {
        public int RatingScaleID { get; set; }
        public string RatingTitle { get; set; } = string.Empty;
        public decimal RatingValue { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class EvaluationCriteriaScoreDto
    {
        [Required]
        public int CriteriaID { get; set; }
        [Required]
        public int RatingScaleID { get; set; }
    }

    // API Request DTO (for frontend)
    public class SubmitEvaluationRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        [Required]
        public int JobApplicationID { get; set; }
        [Required]
        public int ScheduleHeaderId { get; set; }
        [Required]
        public int InterviewerID { get; set; }
        [Required]
        [StringLength(10)]
        public string InterviewRound { get; set; } = string.Empty;
        [Required]
        public DateTime EvaluationDate { get; set; }
        [StringLength(50)]
        public string? Batch { get; set; }
        [Required]
        public int OverallRatingID { get; set; }
        [Required]
        public int RecommendationID { get; set; }
        public string? Comments { get; set; }
        public decimal? EvaluationScore { get; set; }
        [Required]
        public List<EvaluationCriteriaScoreDto> CriteriaScores { get; set; } = new();
        public string? CreatedBy { get; set; }
    }

    // SP Request DTO (for repository - matches SP parameters exactly)
    public class SubmitEvaluationSPRequestDto
    {
        [Required]
        public int ApplicationID { get; set; }
        [Required]
        public int ScheduleID { get; set; }
        [Required]
        public int EvaluatorID { get; set; }
        [Required]
        public int CompanyID { get; set; }
        [Required]
        [Range(0, 10)]
        public decimal OverallRating { get; set; } // DECIMAL(3,2) - 0.00 to 10.00
        [Required]
        [StringLength(50)]
        public string Recommendation { get; set; } = string.Empty; // 'PASS', 'FAIL', 'CONDITIONAL', 'STRONG_PASS'
        public string? Comments { get; set; }
        public string? CreatedBy { get; set; }
        public int RecommendationID { get; set; }
    }

    public class SubmitEvaluationResponseDto
    {
        public int EvaluationID { get; set; }
        public int JobApplicationID { get; set; }
        public int ScheduleHeaderId { get; set; }
        public int InterviewerID { get; set; }
        public string InterviewRound { get; set; } = string.Empty;
        public DateTime EvaluationDate { get; set; }
        public int OverallRatingID { get; set; }
        public int RecommendationID { get; set; }
        public decimal? EvaluationScore { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class EvaluationDto
    {
        public int EvaluationID { get; set; }
        public int ScheduleID { get; set; }
        public int InterviewerID { get; set; }
        public string? InterviewerName { get; set; }
        public int InterviewRound { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public decimal? OverallRating { get; set; }
        public string? Recommendation { get; set; }
        public string? Strengths { get; set; }
        public string? Weaknesses { get; set; }
        public string? Comments { get; set; }
        public bool IsSubmitted { get; set; }
        public DateTime? SubmittedOn { get; set; }
        public List<EvaluationCriteriaScoreDetailDto> CriteriaScores { get; set; } = new();
        public DateTime CreatedOn { get; set; }
    }

    public class EvaluationCriteriaScoreDetailDto
    {
        public int EvaluationID { get; set; }
        public int CriteriaID { get; set; }
        public string? CriteriaTitle { get; set; }
        public int Score { get; set; }
        public string? Comments { get; set; }
    }

    // =============================================
    // MASTER DATA DTOs
    // =============================================

    public class ApplicationSourceDto
    {
        public int SourceID { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class InterviewTypeDto
    {
        public int InterviewTypeID { get; set; }
        public string InterviewTypeName { get; set; } = string.Empty;
        public string InterviewTypeCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class VenueDto
    {
        public int VenueID { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string? VenueAddress { get; set; }
        public bool IsActive { get; set; }
    }

    public class NotificationMethodDto
    {
        public int NotificationMethodID { get; set; }
        public string NotificationMethodName { get; set; } = string.Empty;
        public string NotificationMethodCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ActiveEmployeeDto
    {
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public string? DepartmentName { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
    }

    // =============================================
    // WORKFLOW ACTION DTOs
    // =============================================

    public class ShortlistCandidateRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        public string? ShortlistedBy { get; set; }
        public string? Remarks { get; set; }
    }

    public class ShortlistCandidateResponseDto
    {
        public int ApplicationID { get; set; }
        public int PreviousStatusID { get; set; }
        public string? PreviousStatusCode { get; set; }
        public int NewStatusID { get; set; }
        public string? NewStatusCode { get; set; }
        public string? NewStatusName { get; set; }
        public bool IsShortlisted { get; set; }
        public DateTime ShortlistDate { get; set; }
        public string? ShortlistedBy { get; set; }
    }

    public class RejectApplicationRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        [Required]
        public string RejectionReason { get; set; } = string.Empty;
        public string? RejectedBy { get; set; }
    }

    public class RejectApplicationResponseDto
    {
        public int ApplicationID { get; set; }
        public int PreviousStatusID { get; set; }
        public string? PreviousStatusCode { get; set; }
        public int NewStatusID { get; set; }
        public string? NewStatusCode { get; set; }
        public string? NewStatusName { get; set; }
        public bool IsRejected { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public DateTime RejectedDate { get; set; }
        public string? RejectedBy { get; set; }
    }

    public class HireCandidateRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        public string? OfferLetterPath { get; set; }
        public bool? OfferAccepted { get; set; }
        public string? HiredBy { get; set; }
        public string? Remarks { get; set; }
        public bool OfferLetterBit { get; set; }
        public bool OfferLetterEmailSendBit { get; set; }
        public DateTime? JoiningDate { get; set; }
        public DateTime? OfferDate { get; set; }
        public int? DepartmentID { get; set; }
        public int? DesignationID { get; set; }
        public decimal? Amount { get; set; }
    }

    public class HireCandidateResponseDto
    {
        public int ApplicationID { get; set; }
        public int PreviousStatusID { get; set; }
        public string? PreviousStatusCode { get; set; }
        public int NewStatusID { get; set; }
        public string? NewStatusCode { get; set; }
        public string? NewStatusName { get; set; }
        public bool IsHired { get; set; }
        public string? OfferLetterPath { get; set; }
        public bool? OfferAccepted { get; set; }
        public DateTime HiredDate { get; set; }
        public string? HiredBy { get; set; }
    }

    public class PublishRequisitionRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        public string? PublishedBy { get; set; }
    }

    public class PublishRequisitionResponseDto
    {
        public int RequisitionID { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? PublishedBy { get; set; }
        public int StatusID { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
    }

    public class CancelInterviewScheduleRequestDto
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
        public string? CancelledBy { get; set; }
    }

    public class CancelInterviewScheduleResponseDto
    {
        public int ScheduleID { get; set; }
        public int StatusID { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
        public DateTime CancelledDate { get; set; }
        public string? CancelledBy { get; set; }
    }

    public class CompleteInterviewRoundRequestDto
    {
        public int ApplicationID { get; set; }
        public int ScheduleID { get; set; }
        [Required]
        public int CompanyID { get; set; }
        [Required]
        [StringLength(20)]
        public string Outcome { get; set; } = string.Empty; // "PASSED", "FAILED", "PENDING"
        public int? NextRound { get; set; }
        public string? Comments { get; set; }
        public string? CompletedBy { get; set; }
    }

    public class CompleteInterviewRoundResponseDto
    {
        public int ApplicationID { get; set; }
        public int ScheduleID { get; set; }
        public string Outcome { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string? RoundStatus { get; set; }
        public int? NextRound { get; set; }
        public int TotalInterviewRounds { get; set; }
        public int CurrentInterviewRound { get; set; }
        public int NewStatusID { get; set; }
        public string? NewStatusCode { get; set; }
        public string? NewStatusName { get; set; }
        public ApplicationStatusUpdateDto? ApplicationUpdated { get; set; }
        public DateTime CompletedDate { get; set; }
        public string? CompletedBy { get; set; }
    }

    // =============================================
    // DASHBOARD DTOs
    // =============================================

    public class DashboardStatsDto
    {
        public int TotalRequisitions { get; set; }
        public int ActiveRequisitions { get; set; }
        public int TotalApplications { get; set; }
        public int InterviewsScheduled { get; set; }
        public int HiredCount { get; set; }
        public int PendingEvaluations { get; set; }
        public int TotalJobsAnalyzed { get; set; }
        public int ResumesScreened { get; set; }
        public int CandidatesMatched { get; set; }
        public int TimeSaved { get; set; } // in hours
    }

    public class RecentActivityDto
    {
        public int Id { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? RelatedId { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class DashboardResponseDto
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<RecentActivityDto> RecentActivity { get; set; } = new();
    }

    // =============================================
    // STATUS FILTER DTOs
    // =============================================

    public class GetStatusesByTypeRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        public string? StatusTypeCode { get; set; } // "APPLICATION", "REQUISITION", "SCHEDULE"
        public bool IsActive { get; set; } = true;
    }

    // =============================================
    // UPDATE STATUS DTOs
    // =============================================

    public class UpdateApplicationStatusRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        [Required]
        public int StatusID { get; set; }
        public string? Remarks { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class UpdateApplicationStatusResponseDto
    {
        public int ApplicationID { get; set; }
        public int PreviousStatusID { get; set; }
        public string? PreviousStatusCode { get; set; }
        public int NewStatusID { get; set; }
        public string? NewStatusCode { get; set; }
        public string? NewStatusName { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
    }

    // =============================================
    // FILE UPLOAD DTOs
    // =============================================

    public class UploadResumeRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
    }

    public class UploadResumeResponseDto
    {
        public string RelativePath { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? FileType { get; set; }
    }

    // =============================================
    // PANEL MEMBER EVALUATION DTOs
    // =============================================


    public class PanelMemberScheduleDto
    {
        public int ScheduleID { get; set; }
        public string ScheduleCode { get; set; } = string.Empty;
        public int ApplicationID { get; set; }
        public string ApplicationCode { get; set; } = string.Empty;
        public string? RecommendationStatus { get; set; }
        public string? RecommendationCode { get; set; }
        public int? RecommendationID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public int InterviewRound { get; set; }
        public DateTime ScheduledDate { get; set; }
        public int DurationMinutes { get; set; }

        public string? Venue { get; set; }
        public string? OnlineMeetingLink { get; set; }
        public int PanelID { get; set; }
        public int StatusID { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;

        // GROUPED FIELDS
        public string InterviewersList { get; set; } = string.Empty;
        public string PanelHeadsList { get; set; } = string.Empty;
        public int ConfirmedInterviewerCount { get; set; }
        public int EvaluationSubmitted { get; set; }
        public decimal? AverageScore { get; set; }
    }

    public class PanelMemberScheduleListResponseDto
    {
        public List<PanelMemberScheduleDto> Schedules { get; set; } = new();
        public int TotalRecords { get; set; }
    }

    public class PanelEvaluationRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        [Required]
        public int ScheduleID { get; set; }
        [Required]
        public int InterviewerID { get; set; }
        [Required]
        public int RecommendationID { get; set; }
        public string? Comments { get; set; }
        [Required]
        [Range(0, 5)]
        public decimal OverallRating { get; set; }
        [Required]
        public List<CriteriaRatingDto> CriteriaRatings { get; set; } = new();
        public string? CreatedBy { get; set; }
    }

    public class CriteriaRatingDto
    {
        [Required]
        public int CriteriaID { get; set; }
        [Required]
        public int RatingScaleID { get; set; }
        [Required]
        public int RatingValue { get; set; }
    }

    public class PanelEvaluationResponseDto
    {
        public int EvaluationID { get; set; }
        public int ScheduleID { get; set; }
        public int InterviewerID { get; set; }
        public int RecommendationID { get; set; }
        public string? Comments { get; set; }
        public decimal OverallRating { get; set; }
        public decimal EvaluationScore { get; set; }
        public bool IsSubmitted { get; set; }
        public DateTime? SubmittedOn { get; set; }
        public List<CriteriaRatingDetailDto> CriteriaRatings { get; set; } = new();
    }

    public class CriteriaRatingDetailDto
    {
        public int CriteriaRatingID { get; set; }
        public int CriteriaID { get; set; }
        public string CriteriaCode { get; set; } = string.Empty;
        public string CriteriaTitle { get; set; } = string.Empty;
        public int RatingScaleID { get; set; }
        public int RatingValue { get; set; }
        public string RatingTitle { get; set; } = string.Empty;
    }

    public class ConfirmPanelAttendanceRequestDto
    {
        [Required]
        public string ConfirmedBy { get; set; } = string.Empty;
    }

    public class ConfirmPanelAttendanceResponseDto
    {
        public int PanelID { get; set; }
        public bool IsConfirmed { get; set; }
        public DateTime? ConfirmedOn { get; set; }
    }

    public class RecommendationDto
    {
        public int RecommendationID { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int? ApplicationStatusID { get; set; }
    }

    public class EvaluationCriteriaWithRatingsDto
    {
        public int CriteriaID { get; set; }
        public string CriteriaCode { get; set; } = string.Empty;
        public string CriteriaTitle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<RatingScaleDto> Ratings { get; set; } = new();
    }

    public class ApplicationAIStatusDto
    {
        public int ApplicationID { get; set; }
        public bool IsResumeParsed { get; set; }
        public bool IsScreened { get; set; }
        public decimal? ScreeningScore { get; set; }
        public string? SkillsMatch { get; set; }
        public string? ExperienceMatch { get; set; }
        public string? QualificationsMatch { get; set; }
        public string? Recommendation { get; set; }
        public string? ScreeningMethod { get; set; }
        public string? RedFlags { get; set; }
    }

    public class HireCandidateDto
    {
        public int CompanyID { get; set; }
        public string HiredBy { get; set; }
        public string Remarks { get; set; }
    }

    public class ConvertRequestDto
    {
        public int CompanyID { get; set; }
        public int RequisitionID { get; set; }
        public int JobBankCandidateID { get; set; }
        public string? CreatedBy { get; set; }
    }

}
