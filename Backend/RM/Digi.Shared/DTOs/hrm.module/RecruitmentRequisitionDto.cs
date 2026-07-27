using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.hrm.module
{
    public class RecruitmentRequisitionDto
    {
        public int RecruitmentRequisitionID { get; set; }
        public string? RecruitmentRequisitionCode { get; set; }
        public int? ModuleId { get; set; }
        public int? CompanyID { get; set; }
        public int? ObjectId { get; set; }
        public string? RecruitmentRequisitionName { get; set; }
        public int? BudgetPeriodId { get; set; }
        public string? LocationCode { get; set; }
        public int? ClusterId { get; set; }
        public string? JobCategoryCode { get; set; }
        public string? DesignationCode { get; set; }
        public int? JdId { get; set; }
        public int? Vacancies { get; set; }
        public bool? Replacement { get; set; }
        public string? ReportingPersonCode { get; set; }
        public DateTime? CommenceWorkOn { get; set; }
        public string? EmploymentTypeCode { get; set; }
        public string? GradeCode { get; set; }
        public string? AgeText { get; set; }
        public decimal? ExperienceYears { get; set; }
        public string? QualificationsEntryRequirments { get; set; }
        public string? Exposure { get; set; }
        public string? SkillsRequired { get; set; }
        public string? SpecialAttributes { get; set; }
        public string? Comments { get; set; }
        public string? KeyResponsibilities { get; set; }
        public string? KeyDeliverables { get; set; }
        public string? OtherRequirments { get; set; }
        public string? TechnicalCompetencies { get; set; }
        public string? EducationalQualifications { get; set; }
        public string? EducationalQualificationsDesirable { get; set; }
        public string? RequiredExperiences { get; set; }
        public string? RequiredExperiencesDesirable { get; set; }
        public string? RequiredTrainings { get; set; }
        public string? RequiredTrainingsDesirable { get; set; }
        public string? Justification { get; set; }
        public string? JustificationBy { get; set; }
        public DateTime? JustificationDate { get; set; }
        public bool? ToInternal { get; set; }
        public bool? ToExternal { get; set; }
        public bool? ToThirdParty { get; set; }
        public bool? AlwaysPublished { get; set; }
        public string? PublishStatus { get; set; }
        public DateTime? RecruitmentRequisitionDate { get; set; }
        public DateTime? RecruitmentRequisitionClosingDate { get; set; }
        public bool? NewPublishNotifiedToAll { get; set; }
        public string? PublishedBy { get; set; }
        public DateTime? PublishedDate { get; set; }
        public int? RequestId { get; set; }
        public string? ApprovalStatus { get; set; }
        public bool? IsClosed { get; set; }
        public string? ReplacementEmpType { get; set; }
        public string? AttachedDocument { get; set; }
        public string? CostCenterCode { get; set; }
        public decimal? Salary { get; set; }
        public string? ReasonToDelete { get; set; }
        public bool? Deleted { get; set; }
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }

    public class SaveRecruitmentRequisitionRequest
    {
        public string Action { get; set; } = "INSERT"; // INSERT/UPDATE/DELETE/RESTORE
        public int? RecruitmentRequisitionID { get; set; }
        public string? RecruitmentRequisitionCode { get; set; } // usually null on insert
        //public int? ModuleId { get; set; }
        public int CompanyID { get; set; } // required
        public string? CompanyName { get; set; }
        public int? EmployeeID { get; set; }
        //public int? ObjectId { get; set; }
        public string? RecruitmentRequisitionName { get; set; }
        public int? BudgetPeriodId { get; set; }
        public bool? IsSystemDefault { get; set; } = false;

        public string? Location { get; set; }
       // public int? ClusterId { get; set; }
        public int? JobCategoryID { get; set; }
        public int? DesignationID { get; set; }
       // public int? JdId { get; set; }
        public int? Vacancies { get; set; }
        //public bool? Replacement { get; set; }
        //public string? ReportingPersonCode { get; set; }
        public DateTime? CommenceWorkOn { get; set; }
        public int? EmploymentTypeID { get; set; }
        public int? GradeID { get; set; }
        public int? AgeText { get; set; }
        public int? ExperienceYears { get; set; }
        public string? QualificationsEntryRequirments { get; set; }
        public string? Exposure { get; set; }
        public string? SkillsRequired { get; set; }
        public string? SpecialAttributes { get; set; }
        public string? Comments { get; set; }
        public string? KeyResponsibilities { get; set; }
        public string? KeyDeliverables { get; set; }
        public string? OtherRequirments { get; set; }
        public string? JobSummary { get; set; }
        public string? TechnicalCompetencies { get; set; }
        public string? EducationalQualifications { get; set; }
        public string? EducationalQualificationsDesirable { get; set; }
        public string? RequiredExperiences { get; set; }
        public string? RequiredExperiencesDesirable { get; set; }
        public string? RequiredTrainings { get; set; }
        public string? RequiredTrainingsDesirable { get; set; }
        //public string? Justification { get; set; }
        //public int? JustificationBy { get; set; }
        //public DateTime? JustificationDate { get; set; }
        public bool? ToInternal { get; set; }
        public bool? ToExternal { get; set; }
       // public bool? ToThirdParty { get; set; }
        public bool? AlwaysPublished { get; set; }
        public string? PublishStatus { get; set; }
        public DateTime? RecruitmentRequisitionDate { get; set; }
        public DateTime? RecruitmentRequisitionClosingDate { get; set; }
       // public bool? NewPublishNotifiedToAll { get; set; }
        public string? PublishedBy { get; set; }
        public DateTime? PublishedDate { get; set; }
       // public int? RequestId { get; set; }
        public string? ApprovalStatus { get; set; }
        public bool? IsClosed { get; set; }
        public string? ReplacementEmpType { get; set; }
        public string? AttachedDocument { get; set; }
        public int? DepartmentID { get; set; }
        public decimal? Salary { get; set; }
        public string? ReasonToDelete { get; set; }
       // public string? Status { get; set; }
        public IFormFile? AttachmentFile { get; set; }
        public string? AttachmentURL { get; set; }

        // UserId is filled from server (JWT claim). But allow it too for tests:
        public string EmployeeCode { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;

        public List<AttachmentDto>? attachments { get; set; }
    }


    public class UpdateRecruitmentRequisitionRequest
    {
        public int? RecruitmentRequisitionID { get; set; }
        public string? RecruitmentRequisitionCode { get; set; } // usually null on insert
        public int CompanyID { get; set; } // required
        public string? CompanyName { get; set; }
        public int? EmployeeID { get; set; }
        public string? RecruitmentRequisitionName { get; set; }
        public int? BudgetPeriodId { get; set; }
        public string? Location { get; set; }
        public int? JobCategoryID { get; set; }
        public int? DesignationID { get; set; }
        public int? Vacancies { get; set; }
        public DateTime? CommenceWorkOn { get; set; }
        public int? EmploymentTypeID { get; set; }
        public int? GradeID { get; set; }
        public int? AgeText { get; set; }
        public int? ExperienceYears { get; set; }
        public string? QualificationsEntryRequirments { get; set; }
        public string? Exposure { get; set; }
        public string? SkillsRequired { get; set; }
        public string? SpecialAttributes { get; set; }
        public string? Comments { get; set; }
        public string? KeyResponsibilities { get; set; }
        public string? KeyDeliverables { get; set; }
        public string? OtherRequirments { get; set; }
        public string? TechnicalCompetencies { get; set; }
        public string? EducationalQualifications { get; set; }
        public string? EducationalQualificationsDesirable { get; set; }
        public string? RequiredExperiences { get; set; }
        public string? RequiredExperiencesDesirable { get; set; }
        public string? RequiredTrainings { get; set; }
        public string? RequiredTrainingsDesirable { get; set; }
        public bool? ToInternal { get; set; }
        public bool? ToExternal { get; set; }
        public bool? AlwaysPublished { get; set; }
        public string? PublishStatus { get; set; }
        public DateTime? RecruitmentRequisitionDate { get; set; }
        public DateTime? RecruitmentRequisitionClosingDate { get; set; }
        public string? PublishedBy { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? ApprovalStatus { get; set; }
        public bool? IsClosed { get; set; }
        public string? ReplacementEmpType { get; set; }
      //  public string? AttachedDocument { get; set; }
        public int? DepartmentID { get; set; }
        public decimal? Salary { get; set; }
        public string? ReasonToDelete { get; set; }
        public IFormFile? AttachmentFile { get; set; }
        public string? AttachmentURL { get; set; }
        public int? AttachmentDetailID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;

       public List<AttachmentDto>? attachments { get; set; }
    }


    public class RecruitmentRequisitionGetDto
    {
        public int RecruitmentRequisitionID { get; set; }
        public string RecruitmentRequisitionCode { get; set; }
        public string Position { get; set; }
        public DateTime RequestedDate { get; set; }
        public string RequestedBy { get; set; }
        public int NoOfPositions { get; set; }
        public DateTime? ClosingDate { get; set; }
        public string? PublishStatus { get; set; }
        public bool AlwaysPublished { get; set; }
        public bool IsSystemDefault { get; set; }
        public int CompanyID { get; set; }
        public bool IsClosed { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Location { get; set; }
        public int BudgetPeriodId { get; set; }
        public int EmployeeID { get; set; }
        public int DesignationID { get; set; }
        public int? Jdid { get; set; }
        public DateTime? CommenceWorkOn { get; set; }
        public int EmploymentTypeID { get; set; }
        public int GradeID { get; set; }
        public int AgeText { get; set; }
        public int ExperienceYears { get; set; }
        public string? QualificationsEntryRequirments { get; set; }
        public string? Exposure { get; set; }
        public string? SkillsRequired { get; set; }
        public string? SpecialAttributes { get; set; }
        public string? Comments { get; set; }
        public string? KeyResponsibilities { get; set; }
        public string? KeyDeliverables { get; set; }
        public string? OtherRequirments { get; set; }
        public string? TechnicalCompetencies { get; set; }
        public string? EducationalQualifications { get; set; }
        public string? EducationalQualificationsDesirable { get; set; }
        public string? RequiredExperiences { get; set; }
        public string? RequiredExperiencesDesirable { get; set; }
        public string? RequiredTrainings { get; set; }
        public string? RequiredTrainingsDesirable { get; set; }
        public string? ApprovalStatus { get; set; }
        public int? DepartmentID { get; set; }
        public int Salary { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? RecruitmentRequisitionDate { get; set; }
        public DateTime? RecruitmentRequisitionClosingDate { get; set; }
        public string? PublishedBy { get; set; }
        public int JobCategoryID { get; set; }
        public string FilePath { get; set; }
        public  int? AttachmentDetailID { get; set; }
    }

    public class RecruitmentSummaryDto
    {
        public string? Status { get; set; }
        public int CountValue { get; set; }
    }

    public class CandidateAssignGroupDto
    {
        public int RecruitmentRequisitionID { get; set; }
        public string Position { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int? ResidualDays { get; set; }
        public int? NoOfVacancies { get; set; }
        public int? TotalApplicants { get; set; }
        public int? ShortlistedTotal { get; set; }
        public int? TotalHold { get; set; }
        public int? RejectedTotal { get; set; }
    }

    public class CandidateEvaluationDto
    {
        public long EvaluationId { get; set; }
        public long JobApplicationId { get; set; }
        public int ScheduleId { get; set; }
        public int InterviewerId { get; set; }
        public int InterviewRound { get; set; }
        public DateTime EvaluationDate { get; set; }
        public string Batch { get; set; }
        public int? OverallRatingId { get; set; }
        public string Recommendation { get; set; }
        public string Comments { get; set; }
        public decimal? EvaluationScore { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdateBy { get; set; }

        // Candidate Info
        public int ApplicantId { get; set; }
        public string ApplicantCode { get; set; }
        public string CandidateName { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }

        // Job Application Info
        public long JobRequisitionId { get; set; }
        public string RecruitmentRequisitionCode { get; set; }

        // Venue Info
        public string VenueCode { get; set; }
        //public string VenueName { get; set; }
        //public string VenueAddress { get; set; }

        // Schedule Info
        public DateTime InterviewDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // Derived Status
        public string Status { get; set; }
        public string EvaluationStatus { get; set; }
    }
    public class SchedulePanelAssignListDto
    {
        public int PanelId { get; set; }
        public int ScheduleHeaderId { get; set; }
        public DateTime InterviewDate { get; set; }
        public int InterviewDuration { get; set; }


        public int NotificationMethodID { get; set; }
        public string NotificationMethod { get; set; }


        public int ScheduleStageID { get; set; }
        public string ScheduleStage { get; set; }
        
        public int InterviewStateID { get; set; }
        public string InterViewState { get; set; }

        public int VenueID { get; set; }
        public string Venue { get; set; }

        public int RecommendationID { get; set; }
        public string Recommendation { get; set; }

        public int DepartmentID { get; set; }
        public string DeparmentName { get; set; }


        public int InterviewerId { get; set; }
        public string InterviewerName { get; set; }
        public string RecruitmentRequisitionName { get; set; }
        public int JobApplicationID { get; set; }
        public int ApplicantID { get; set; }
        public string CandidateName { get; set; }
        public string Comments { get; set; }
        public decimal? EvaluationScore { get; set; }
        public bool IsHead { get; set; }
        public bool IsActive { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? UpdatedByName { get; set; }
        public int AttachmentDetailID { get; set; }
        public string? EmployeeImage { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public List<CandidateEvaluationCriteria_V1Dto> EvaluationCriteria { get; set; } = new();
    }


    public class ScheduleAssignInterviewListDto
    {
       
        public int PanelId { get; set; }
        public int InterviewerId { get; set; }
        public string? InterviewerName { get; set; }
        public string? CandidateName { get; set; }
        public DateTime? InterviewDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? NotificationMethodID { get; set; }
        public string? NotificationMethod { get; set; }
        public int? InterviewStateID { get; set; }
        public string? InterViewState { get; set; }
        public int? VenueID { get; set; }
        public string? Venue { get; set; }
        public string? RecruitmentRequisitionName { get; set; }
        public bool? IsHead { get; set; }
        public int? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public string? EmployeeImage { get; set; }
        public int? AttachmentDetailID { get; set; }
        public decimal? EvaluationScore { get; set; }
        public int? JobApplicationID { get; set; }
        public int? ScheduleHeaderId { get; set; }

    }
    //public class SchedulePanelAssignList_V1Dto
    //{
    //    public int ScheduleHeaderId { get; set; }
    //    public string? CandidateName { get; set; }
    //    public int InterviewerID { get; set; }
    //    public int CompanyID { get; set; }

    //    // Child List
    //    public List<CandidateEvaluationCriteria_V1Dto> EvaluationCriteria { get; set; } = new();
    //}

    public class CandidateEvaluationCriteria_V1Dto
    {
        public int EvaluationDetailID { get; set; }
        public int CompanyID { get; set; }
        public int EvaluationID { get; set; }
        public int CriteriaID { get; set; }
        public int RatingScaleID { get; set; }
    }

}
