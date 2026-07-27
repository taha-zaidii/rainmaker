using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.hrm.module
{
    public class RecruitmentJobDetailDto
    {
        public string JobTitle { get; set; }
        public string JobLocation { get; set; }
        public string JobType { get; set; }
        public int CompanyID { get; set; }
        public string DesignationCode { get; set; }
        public int JdId { get; set; }
        public int Vacancies { get; set; }
        public DateTime? StartDate { get; set; }
        public string AgeText { get; set; }
        public int? ExperienceYears { get; set; }
        public string QualificationRequired { get; set; }
        public string Exposure { get; set; }
        public string SkillsRequired { get; set; }
        public string SpecialAttributes { get; set; }
        public string KeyResponsibilities { get; set; }
        public string KeyDeliverables { get; set; }
        public string OtherRequirments { get; set; }
        public string TechnicalCompetencies { get; set; }
        public string EducationalQualifications { get; set; }
        public string EducationalQualificationsDesirable { get; set; }
        public string RequiredExperiences { get; set; }
        public string RequiredExperiencesDesirable { get; set; }
        public string RequiredTrainings { get; set; }
        public string RequiredTrainingsDesirable { get; set; }
        public string AdditionalComments { get; set; }
        public string Justification { get; set; }
        public decimal? Salary { get; set; }
        public string PublishStatus { get; set; }
        public DateTime? RecruitmentRequisitionDate { get; set; }
        public DateTime? RecruitmentRequisitionClosingDate { get; set; }
        public bool IsClosed { get; set; }
        public bool IsSystemDefault { get; set; }
    }
    public class JobApplicationByShortListedDto
    {
        public int? JobApplicationID { get; set; }
        public int? JobRequisitionID { get; set; }
        public int? ApplicantID { get; set; }
        public string? ApplicantName { get; set; }
        public int? ApplicationStatusID { get; set; }
        public string? JobApplicationStatus { get; set; }
        public string? ResumePath { get; set; }
        public decimal? Rating { get; set; }
        public bool IsShortlisted { get; set; }
        public string? Comments { get; set; }
        public int? ScheduleStageID { get; set; }
        public string ScheduleStage { get; set; }
        public int? InterviewStateID { get; set; }
        public string InterViewState { get; set; }

        //public bool ToInternal { get; set; }
        //public bool ToExternal { get; set; }



        //public DateTime? InterviewDate { get; set; }
        // public string InterviewFeedback { get; set; }
        //public string Notes { get; set; }
        //public string? FilePath { get; set; }
        //public bool IsHired { get; set; }
        //public int? InterviewCount { get; set; }
    }
    public class JobApplicationDto
    {
        public int? JobApplicationID { get; set; }
        public int? JobRequisitionID { get; set; }
        public int?  ApplicantID { get; set; }
        public string? ApplicatName { get; set; }
        public int? ApplicationStatusID { get; set; }
        public string? JobApplicationStatus { get; set; }
        public string? ResumePath { get; set; }
        public decimal? Rating { get; set; }
        public bool IsShortlisted { get; set; }
        public string? Comments { get; set; }
        public int? ScheduleStageID { get; set; }
        public string? ScheduleStage { get; set; }
        public int? InterviewStateID { get; set; }

        //public bool ToInternal { get; set; }
        //public bool ToExternal { get; set; }
        //public string ScheduleStage { get; set; }
        //public string InterViewState { get; set; }
        //public DateTime? InterviewDate { get; set; }
        // public string InterviewFeedback { get; set; }
        //public string Notes { get; set; }
        //public string? FilePath { get; set; }
        //public bool IsHired { get; set; }
        //public int? InterviewCount { get; set; }
    }
    public class UpdateJobApplicationStatusDto
    {
        public int JobApplicationID { get; set; }
        public int? ScheduleStageID { get; set; }
       // public string InterviewFeedback { get; set; }
        public string Remarks { get; set; }
        public decimal? ScreeningScore { get; set; }
        public bool? IsShortlisted { get; set; }
       // public bool? IsHired { get; set; }
         public bool? IsRejected { get; set; }
        public string UpdatedBy { get; set; }
        public int? InterviewStateID { get; set; }
       // public int? ApplicationStateID { get; set; }
    }

    public class RecruitmentRequisitionActionDto
    {
        public string Action { get; set; }   // "PUBLISH", "UNPUBLISH", "CLOSE", "GET"
        public int? RecruitmentRequisitionID { get; set; }
        public string EmployeeCode { get; set; }
        public int CompanyID { get; set; }
    }
    public class RecruitmentRequisitionPublicDto
    {
        public int RecruitmentRequisitionID { get; set; }
        public int JobCategoryID { get; set; }
        public string RecruitmentRequisitionCode { get; set; }
        public string RecruitmentRequisitionName { get; set; }
        public int Vacancies { get; set; }
        public string Location { get; set; }
        public DateTime RecruitmentRequisitionDate { get; set; }
        public DateTime RecruitmentRequisitionClosingDate { get; set; }
        public string PublishStatus { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string PublishedBy { get; set; }
        public bool IsClosed { get; set; }
        public bool IsSystemDefault { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? KeyResponsibilities { get; set; }
        public decimal? Salary { get; set; }
        public string? AgeText { get; set; }
        public int? ExperienceYears { get; set; }
    }

    public class ApplicationStatusDto
    {
        public int ApplicationStatusID { get; set; }
        public string StatusName { get; set; }
        public string ApplicationStatusName { get; set; } = string.Empty;
        public string? ApplicationStatusCode { get; set; }
        public string? StatusCode { get; set; }
    }

    public class ShortlistedCandidateDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? ApplicantCode { get; set; }
        public int ScheduleHeaderId { get; set; }
        public int JobApplicationID { get; set; }
        public int ApplicantID { get; set; }
        public int CompanyID { get; set; }
        public int InterviewStateID { get; set; }
        public string? InterviewState { get; set; }


        // public string Email { get; set; }
        // public string MobileNumber { get; set; }
        //public DateTime ApplicationDate { get; set; }
        //public int ApplicationStatusID { get; set; }
        //public string ApplicationStatus { get; set; }
        //public int ApplicationStateID { get; set; }
        //public string ApplicationState { get; set; }
        //public int NotificationMethodID { get; set; }
        //public string NotificationMethodName { get; set; }
        //public int ScheduleInterviewStateID { get; set; }
        //public string ScheduleInterviewStateName { get; set; }
        //public int VenueID { get; set; }
        //public string VenueName { get; set; }
        //public DateTime InterviewDate { get; set; }
        //public int InterviewDuration { get; set; }
        //public TimeSpan StartTime { get; set; }

        //public int RecommendationID { get; set; }
        //public int InterviewCount { get; set; }
        //public bool IsScheduled { get; set; }
        //public bool IsShortlisted { get; set; }
        //public bool ToInternal { get; set; }
        //public bool ToExternal { get; set; }
        //public bool IsHired { get; set; }
        //public string JobStage { get; set; }
        //public int ScheduleStageID { get; set; }
    }
    public class CandidateDto
    {
        public int? ScheduleHeaderId { get; set; }
        public int? ApplicantID { get; set; }
        public int? JobApplicationID { get; set; }
        public DateTime? InterviewDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public int? Duration { get; set; }
        public int InterviewStateID { get; set; }
        public int? NotificationMethodID { get; set; }
        public int? VenueID { get; set; }
    }
    public class PanelMemberDto
    {
        public int? InterviewerId { get; set; }
        public bool? IsHead { get; set; }
    }
    public class InterviewScheduleRequestDto
    {
        public int? CompanyID { get; set; }
        public string? CreatedBy { get; set; }

        // Bulk data - each candidate has its own schedule details (including ScheduleHeaderId, NotificationMethodID and VenueID)
        public List<CandidateDto> Candidates { get; set; }
        public List<PanelMemberDto> PanelMembers { get; set; }
    }
    public class InterviewPanelMemberDto
    {
        public int ScheduleHeaderId { get; set; }
        public int PanelId { get; set; }
        public int InterviewerId { get; set; }
        public string EmployeeCode { get; set; }
        public string InterviewerName { get; set; }
        public bool IsHead { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class InterviewScheduleDto
    {
        //public int ScheduleHeaderId { get; set; }
        //public string RecruitmentRequisitionCode { get; set; }
        //public string ActivityCode { get; set; }
        //public string ScheduleStage { get; set; }

        //public DateTime HeaderInterviewDate { get; set; }
        //public TimeSpan HeaderStartTime { get; set; }
        //public int HeaderDuration { get; set; }
        //public string HeaderNotificationMethod { get; set; }
        //public string HeaderTemplate { get; set; }
        //public string HeaderVenue { get; set; }

        //public int ScheduleId { get; set; }
        //public string JobAppCode { get; set; }
        //public int JobApplicationId { get; set; }
        //public string FirstName { get; set; }
        //public string Email { get; set; }
        //public string MobileNumber { get; set; }

        //public DateTime InterviewDate { get; set; }
        //public TimeSpan StartTime { get; set; }
        //public TimeSpan EndTime { get; set; }
        //public int Duration { get; set; }
        //public string NotificationMethod { get; set; }
        //public string NotificationTemplateCode { get; set; }
        //public string VenueCode { get; set; }

        //public int CreatedBy { get; set; }
        //public DateTime CreatedDate { get; set; }
        //public int? UpdatedBy { get; set; }
        //public DateTime? UpdatedDate { get; set; }
        //public bool IsActive { get; set; }
        public int ScheduleHeaderId { get; set; }

        // Header Information
        public string ActivityCode { get; set; }
        public int ScheduleStageId { get; set; }
        public DateTime? HeaderInterviewDate { get; set; }
        public TimeSpan? HeaderStartTime { get; set; }
        public int? HeaderDuration { get; set; }
        public int? HeaderNotificationMethod { get; set; }
        public string NotificationName { get; set; }

        public int? HeaderTemplate { get; set; }
        public string InterviewState { get; set; }
        public int? HeaderVenue { get; set; }
        public string VenuName { get; set; }

        // Schedule & Applicant Info
        public int ScheduleId { get; set; }
        public int ApplicantId { get; set; }
        public int JobApplicationId { get; set; }
        public int ApplicationStatusId { get; set; }
        public bool? IsHired { get; set; }
        public string ApplicationStatus { get; set; }

        // Candidate Info
        public string FirstName { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }

        // Candidate Evaluation
        public string Batch { get; set; }
        public decimal? EvaluationScore { get; set; }
        public DateTime? EvaluationDate { get; set; }
        public int? RecommendationId { get; set; }
        public string Recommendation { get; set; }
        public List<InterviewPanelMemberDto> PanelMembers { get; set; } = new();
    }
    public class InterviewScheduleCollectionDto
    {
        public List<InterviewScheduleDto> Schedules { get; set; }
    }

}
