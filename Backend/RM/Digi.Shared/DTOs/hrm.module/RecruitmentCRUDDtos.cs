using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.hrm.module
{
    // =============================================
    // APPLICANT CRUD DTOs
    // =============================================

    public class ApplicantCreateRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        //[Required]
        //[StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        //[StringLength(100)]
        public string? MiddleName { get; set; }
        
        //[Required]
        //[StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        public DateTime? DateOfBirth { get; set; }
        public int? GenderID { get; set; }
        
        [StringLength(50)]
        public string? NationalID { get; set; }
        
        //[Required]
        //[StringLength(150)]
        //[EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        //[StringLength(20)]
        public string? MobileNumber { get; set; }
        
        //[StringLength(20)]
        public string? PhoneNumber { get; set; }
        public string? Cnic { get; set; }
        //[StringLength(300)]
        public string? CurrentAddress { get; set; }
        
        //public IList<IFormFile>? UploadResume { get; set; }
        public int? CityID { get; set; }
        public int? CountryID { get; set; }
        public string? Skills { get; set; }
        public int? MaritalStatusID { get; set; }
        public int? ExperienceYears { get; set; }
        public string? ExperienceSummary { get; set; }
        public string? Education { get; set; }
        public string? CurrentDesignation { get; set; }
        public string? PreferredLocation { get; set; }
        public int? ReligionID { get; set; }
        public decimal? TotalExperience { get; set; }
        
        //[StringLength(150)]
        public string? CurrentJobTitle { get; set; }
        
        //[StringLength(200)]
        public string? CurrentCompany { get; set; }
        
        public decimal? ExpectedSalary { get; set; }
        public int? NoticePeriod { get; set; }
        public string? ResumePath { get; set; }
        public string? CoverLetter { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class ApplicantUpdateRequestDto
    {
        [Required]
        public int ApplicantID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? MiddleName { get; set; }
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        public DateTime? DateOfBirth { get; set; }
        public int? GenderID { get; set; }
        
        [StringLength(50)]
        public string? NationalID { get; set; }
        
        [Required]
        [StringLength(150)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? MobileNumber { get; set; }
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        [StringLength(300)]
        public string? CurrentAddress { get; set; }
        
        public int? CityID { get; set; }
        public int? CountryID { get; set; }
        public int? MaritalStatusID { get; set; }
        public int? ReligionID { get; set; }
        public decimal? TotalExperience { get; set; }
        
        [StringLength(150)]
        public string? CurrentJobTitle { get; set; }
        
        [StringLength(200)]
        public string? CurrentCompany { get; set; }
        
        public decimal? ExpectedSalary { get; set; }
        public int? NoticePeriod { get; set; }
        public bool? IsActive { get; set; }
       
    }

    public class ApplicantResponseDto
    {
        public int ApplicantID { get; set; }
        public string? ApplicantCode { get; set; }
        public int CompanyID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? GenderID { get; set; }
        public string? NationalID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? MobileNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Cnic { get; set; }
        public string? CurrentAddress { get; set; }
        public int? CityID { get; set; }
        public int? CountryID { get; set; }
        public int? MaritalStatusID { get; set; }
        public int? ReligionID { get; set; }
        public decimal? TotalExperience { get; set; }
        public string? CurrentJobTitle { get; set; }
        public string? CurrentCompany { get; set; }
        public decimal? ExpectedSalary { get; set; }
        public int? NoticePeriod { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? ExperienceYears { get; set; }
        public string? ExperienceSummary { get; set; }
        public string? Education { get; set; }
        public string? CurrentDesignation { get; set; }
        public string? PreferredLocation { get; set; }
    }

    public class ApplicantListRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ApplicantListResponseDto
    {
        public List<ApplicantResponseDto> Applicants { get; set; } = new List<ApplicantResponseDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    // =============================================
    // JOB REQUISITION CRUD DTOs
    // =============================================

    public class JobRequisitionCreateRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string JobTitle { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? JobSummary { get; set; }
        
        public int? DepartmentID { get; set; }
        public int? DesignationID { get; set; }
        public int? EmploymentTypeID { get; set; }
        public int? GradeID { get; set; }
        
        public int Vacancies { get; set; } = 1;
        
        public int? MinExperience { get; set; }
        public int? MaxExperience { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        
        [StringLength(200)]
        public string? Location { get; set; }
        
        public int? ReportingTo { get; set; }
        
        public string? KeyResponsibilities { get; set; }
        public string? Requirements { get; set; }
        public string? Qualifications { get; set; }
        public string? Skills { get; set; }
        public string? Benefits { get; set; }
        public string? Justification { get; set; }
        public bool? IsPublic { get; set; }
        public bool? IsDefault { get; set; }
        public bool IsPublished { get; set; } = false;
        public DateTime? PublishedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int? StatusID { get; set; }
        public int? JobCategoryID { get; set; }
        public bool? Isbudget { get; set; }
        public bool? IsNonBudget { get; set; }
        public int? SalaryRecommendationID { get; set; }
        public string? CreatedBy { get; set; }

        /// <summary>NEW_JOINING or REPLACEMENT hiring detail</summary>
        public JobRequisitionHiringDetailDto? HiringDetail { get; set; }
    }

    public class JobRequisitionUpdateRequestDto
    {
        [Required]
        public int RequisitionID { get; set; }
        
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string JobTitle { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? JobSummary { get; set; }
        
        public int? DepartmentID { get; set; }
        public int? DesignationID { get; set; }
        public int? EmploymentTypeID { get; set; }
        public int? GradeID { get; set; }
        
        public int? Vacancies { get; set; }
        
        public int? MinExperience { get; set; }
        public int? MaxExperience { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        
        [StringLength(200)]
        public string? Location { get; set; }
        
        public int? ReportingTo { get; set; }
        
        public string? KeyResponsibilities { get; set; }
        public string? Requirements { get; set; }
        public string? Qualifications { get; set; }
        public string? Skills { get; set; }
        public string? Benefits { get; set; }
        public string? Justification { get; set; }
        
        public bool? IsPublic { get;set;}
        public bool? IsPublished { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int? StatusID { get; set; }
        public int? JobCategoryID { get; set; }
        public bool? Isbudget { get; set; }
        public bool? IsNonBudget { get; set; }
        public bool? IsActive { get; set; }
        public int? SalaryRecommendationID { get; set; }

        /// <summary>NEW_JOINING or REPLACEMENT hiring detail</summary>
        public JobRequisitionHiringDetailDto? HiringDetail { get; set; }
    }

    public class JobRequisitionHiringDetailDto
    {
        public int? HiringDetailID { get; set; }
        public int? RequisitionID { get; set; }
        public int? CompanyID { get; set; }

        /// <summary>NEW_JOINING | REPLACEMENT</summary>
        [StringLength(30)]
        public string HiringType { get; set; } = "NEW_JOINING";

        public int? ReplacedEmployeeID { get; set; }

        /// <summary>RESIGNED | TERMINATED | TRANSFERRED | RETIRED | OTHER</summary>
        [StringLength(50)]
        public string? ReplacementReason { get; set; }

        public DateTime? LastWorkingDate { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        public string? ReplacedEmployeeName { get; set; }
        public bool IsActive { get; set; } = true;
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class JobRequisitionResponseDto
    {
        public int RequisitionID { get; set; }
        public string? RequisitionCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? EmployeeName { get; set; }
        public int CompanyID { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string? JobSummary { get; set; }
        public int? DepartmentID { get; set; }
        public int? DesignationID { get; set; }
        public int? EmploymentTypeID { get; set; }
        public string? EmploymentTypeName { get; set; }
        public int? GradeID { get; set; }
        public int Vacancies { get; set; }
        public int? MinExperience { get; set; }
        public int? MaxExperience { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string? Location { get; set; }
        public int? ReportingTo { get; set; }
        public string? KeyResponsibilities { get; set; }
        public string? Requirements { get; set; }
        public string? Qualifications { get; set; }
        public string? Skills { get; set; }
        public string? Benefits { get; set; }
        public string? Justification { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int? StatusID { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
        public int? JobCategoryID { get; set; }
        public bool? Isbudget { get; set; }
        public bool? IsNonBudget { get; set; }
        public bool IsActive { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsPublic { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? SalaryRecommendationID { get; set; }
        public int? TotalApplications { get; set; }
        public string? FilePath { get; set; }

        public JobRequisitionHiringDetailDto? HiringDetail { get; set; }
        public string? HiringType { get; set; }
        public int? ReplacedEmployeeID { get; set; }
        public string? ReplacementReason { get; set; }
        public DateTime? LastWorkingDate { get; set; }
        public string? HiringRemarks { get; set; }
    }

    public class JobRequisitionListRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SearchTerm { get; set; }
        public int? StatusID { get; set; }
        public bool? IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public int? DepartmentID { get; set; }
    }

    public class JobRequisitionListResponseDto
    {
        public List<JobRequisitionResponseDto> Requisitions { get; set; } = new List<JobRequisitionResponseDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    // =============================================
    // JOB APPLICATION CRUD DTOs
    // =============================================

    public class JobApplicationCreateRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        public int RequisitionID { get; set; }
        
        [Required]
        public int ApplicantID { get; set; }

        public DateTime? ApplicationDate { get; set; } = DateTime.Now;
        public int? ApplicationSourceID { get; set; }
        public int? CurrentStatusID { get; set; }
        
        //[StringLength(500)]
        public string? ResumePath { get; set; }
        
        public string? CoverLetter { get; set; }
        
        //[StringLength(1000)]
        public string? Remarks { get; set; }
    }

    public class JobApplicationUpdateRequestDto
    {
        [Required]
        public int ApplicationID { get; set; }
        
        public int? CurrentStatusID { get; set; }
        
        [StringLength(500)]
        public string? ResumePath { get; set; }
        
        public string? CoverLetter { get; set; }
        
        public decimal? ScreeningScore { get; set; }
        public decimal? OverallRating { get; set; }
        
        [StringLength(50)]
        public string? FinalRecommendation { get; set; }
        
        [StringLength(500)]
        public string? RejectionReason { get; set; }
        
        [StringLength(500)]
        public string? OfferLetterPath { get; set; }
        
        public bool? OfferAccepted { get; set; }
        
        [StringLength(1000)]
        public string? Remarks { get; set; }
        
        public bool? IsActive { get; set; }
    }

    public class JobApplicationResponseDto
    {
        public int ApplicationID { get; set; }
        public string? ApplicationCode { get; set; }
        public int CompanyID { get; set; }
        public int RequisitionID { get; set; }
        public int ApplicantID { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public int? ApplicationSourceID { get; set; }
        public int? CurrentStatusID { get; set; }
        public int? StatusID { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
        public string? ApplyCode { get; set; }
        public string? ApplicantStatus { get; set; }
        public string? ResumePath { get; set; }
        public string? CoverLetter { get; set; }
        public decimal? ScreeningScore { get; set; }
        public decimal? OverallRating { get; set; }
        public string? Recommendation { get; set; }
        public string? RejectionReason { get; set; }
        public string? OfferLetterPath { get; set; }
        public bool? OfferAccepted { get; set; }
        public string? Remarks { get; set; }
        public int? ResumeParsingID { get; set; }
        public int? CandidateRankingID { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        // Related Data
        public string? ApplicantCode { get; set; }
        public string? DepartmentName { get; set; }
        //public string? ApplicantFirstName { get; set; }
        //public string? ApplicantLastName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? MobileNumber { get; set; }
        public string? RequisitionCode { get; set; }
        public string? RequisitionJobTitle { get; set; }
        public string? RequisitionLocation { get; set; }

        public bool OfferLetterBit { get; set; }
        public bool OfferLetterEmailSendBit { get; set; }

        public DateTime? JoiningDate { get; set; }
        public DateTime? OfferDate { get; set; }
        public int? DepartmentID { get; set; }
        public int? DesignationID { get; set; }
        public decimal? Amount { get; set; }

        public string? CurrentAddress { get; set; }
        public string? ExperienceSummary { get; set; }
        public string? CurrentDesignation { get; set; }

        public string? CurrentCompany { get; set; }
        public string? Skills { get; set; }
        public string? PreferredLocation { get; set; }
        public string? Education { get; set; }

        public string? DateOfBirth { get; set; }
        public string? ExpectedSalary { get; set; }
        public decimal? ExperienceYears { get; set; }
        public string? NoticePeriod { get; set; }
        public decimal? TotalExperience { get; set; }

        public string? CurrentJobTitle { get; set; }
    }

    public class JobApplicationListRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int? RequisitionID { get; set; }
        public int? ApplicantID { get; set; }
        public int? CurrentStatusID { get; set; }
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
    }

    public class JobApplicationListResponseDto
    {
        public List<JobApplicationResponseDto> Applications { get; set; } = new List<JobApplicationResponseDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    // =============================================
    // INTERVIEW SCHEDULE CRUD DTOs
    // =============================================

    public class InterviewScheduleCreateRequestDto
    {
        [Required]
        public int ApplicationID { get; set; }
        
        [Required]
        public int InterviewRound { get; set; }
        
        public int? InterviewTypeID { get; set; }
        
        [Required]
        public DateTime ScheduledDate { get; set; }
        
        public int DurationMinutes { get; set; } = 60;
        
        [StringLength(200)]
        public string? Venue { get; set; }
        
        [StringLength(500)]
        public string? OnlineMeetingLink { get; set; }
        
        [StringLength(500)]
        public string? Instructions { get; set; }
        
        public int? StatusID { get; set; }
        public List<WorkflowPanelMemberDto> PanelMembers { get; set; } = new();

    }

    public class InterviewScheduleUpdateRequestDto
    {
        [Required]
        public int ScheduleID { get; set; }
        
        public int? InterviewRound { get; set; }
        public int? InterviewTypeID { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public int? DurationMinutes { get; set; }
        
        [StringLength(200)]
        public string? Venue { get; set; }
        
        [StringLength(500)]
        public string? OnlineMeetingLink { get; set; }
        
        [StringLength(500)]
        public string? Instructions { get; set; }
        
        public int? StatusID { get; set; }
        public bool? IsNotified { get; set; }
        public DateTime? NotificationSentOn { get; set; }
        public string? FeedbackSummary { get; set; }
        public bool? IsActive { get; set; }
    }

    public class InterviewScheduleResponseDto
    {
        public int ScheduleID { get; set; }
        public string? ScheduleCode { get; set; }
        public int ApplicationID { get; set; }
        public int InterviewRound { get; set; }
        public int? InterviewTypeID { get; set; }
        public DateTime ScheduledDate { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Venue { get; set; }
        public string? OnlineMeetingLink { get; set; }
        public string? Instructions { get; set; }
        public int? StatusID { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
        public bool IsNotified { get; set; }
        public DateTime? NotificationSentOn { get; set; }
        public string? FeedbackSummary { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        // Related Data
        public string? ApplicationCode { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public string? ApplicantCode { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? ApplicantMobileNumber { get; set; }
        public string? RequisitionCode { get; set; }
        public string? JobTitle { get; set; }
        public bool? IsInterviewScheduled { get; set; }
        public List<InterviewPanelDto> PanelMembers { get; set; } = new();


    }

    public class InterviewPanelDto
    {
        public int PanelID { get; set; }
        public int ScheduleID { get; set; }
        public int InterviewerID { get; set; }
        public string? EmployeeCode { get; set; }
        public string? InterviewerName { get; set; }
        public string? InterviewerEmail { get; set; }
        public bool IsPanelHead { get; set; }
        public bool IsRequired { get; set; }
        public bool IsConfirmed { get; set; }
        public DateTime? ConfirmedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
    }


    public class InterviewScheduleListRequestDto
    {
        public int? CompanyID { get; set; }
        public int? ApplicationID { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int? StatusID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsActive { get; set; }
    }

    public class InterviewScheduleListResponseDto
    {
        public List<InterviewScheduleResponseDto> Schedules { get; set; } = new List<InterviewScheduleResponseDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    // =============================================
    // STATUS MANAGEMENT DTOs
    // =============================================

    public class StatusResponseDto
    {
        public int StatusID { get; set; }
        public int StatusTypeID { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public string? StatusTypeCode { get; set; }
        public string? StatusTypeName { get; set; }
    }

    public class StatusTypeResponseDto
    {
        public int StatusTypeID { get; set; }
        public string TypeCode { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }


    public class EvaluationResponseDtos
    {
        public int EvaluationID { get; set; }
        public int ScheduleID { get; set; }
        public decimal OverallRating { get; set; }
        public int RecommendationID { get; set; }
        public string? Recommendation { get; set; }
        public string? Comments { get; set; }
        public bool IsSubmitted { get; set; }
        public DateTime? SubmittedOn { get; set; }

        public List<EvaluationCriteriaScoreDtos> CriteriaScores { get; set; } = new();
    }

    public class EvaluationCriteriaScoreDtos
    {
        public int CriteriaID { get; set; }
        public string CriteriaTitle { get; set; } = string.Empty;
        public int RatingScaleID { get; set; }
        public string RatingTitle { get; set; } = string.Empty;
        public int RatingValue { get; set; }
    }
}
