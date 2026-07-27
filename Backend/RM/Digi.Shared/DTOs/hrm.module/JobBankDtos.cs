using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.hrm.module
{
    // =============================================
    // JOB BANK CANDIDATE DTOs
    // =============================================

    /// <summary>
    /// Request to register a candidate in the job bank (POST JobBank/Candidates).
    /// </summary>
    public class JobBankCandidateInsertRequestDto
    {
        [Required]
        public int CompanyID { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(50)]
        public string? Cnic { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public int? GenderID { get; set; }

        [StringLength(500)]
        public string? CurrentAddress { get; set; }
        public int? CityID { get; set; }
        public int? CountryID { get; set; }

        [StringLength(500)]
        public string? ResumeFilePath { get; set; }

        [StringLength(255)]
        public string? ResumeFileName { get; set; }

        [StringLength(1000)]
        public string? Skills { get; set; }
        public int? ExperienceYears { get; set; }

        [StringLength(2000)]
        public string? ExperienceSummary { get; set; }

        [StringLength(1000)]
        public string? Education { get; set; }

        [StringLength(150)]
        public string? CurrentDesignation { get; set; }
        public decimal? ExpectedSalary { get; set; }

        [StringLength(200)]
        public string? PreferredLocation { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Request to update a job bank candidate (PUT JobBank/Candidates/{id}). Excludes CompanyID.
    /// </summary>
    public class JobBankCandidateUpdateRequestDto
    {
        [Required]
        public int JobBankCandidateID { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(50)]
        public string? Cnic { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public int? GenderID { get; set; }

        [StringLength(500)]
        public string? CurrentAddress { get; set; }
        public int? CityID { get; set; }
        public int? CountryID { get; set; }

        [StringLength(500)]
        public string? ResumeFilePath { get; set; }

        [StringLength(255)]
        public string? ResumeFileName { get; set; }

        [StringLength(1000)]
        public string? Skills { get; set; }
        public int? ExperienceYears { get; set; }

        [StringLength(2000)]
        public string? ExperienceSummary { get; set; }

        [StringLength(1000)]
        public string? Education { get; set; }

        [StringLength(150)]
        public string? CurrentDesignation { get; set; }
        public decimal? ExpectedSalary { get; set; }

        [StringLength(200)]
        public string? PreferredLocation { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Response after registering a candidate (POST JobBank/Candidates).
    /// </summary>
    public class JobBankCandidateInsertResponseDto
    {
        public int JobBankCandidateID { get; set; }
    }

    /// <summary>
    /// Single job bank candidate response (get by id / list item).
    /// </summary>
    public class JobBankCandidateResponseDto
    {
        public int JobBankCandidateID { get; set; }
        public int? RequisitionID { get; set; }
        public bool IsShortlisted { get; set; }
		
        public int? ApplicationID { get; set; }
        public int CompanyID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Cnic { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? GenderID { get; set; }
        public string? CurrentAddress { get; set; }
        public int? CityID { get; set; }
        public int? CountryID { get; set; }
        public string? ResumeFilePath { get; set; }
        public string? ResumeFileName { get; set; }
        public string? Skills { get; set; }
        public int? ExperienceYears { get; set; }
        public string? ExperienceSummary { get; set; }
        public string? Education { get; set; }
        public string? CurrentDesignation { get; set; }
        public decimal? ExpectedSalary { get; set; }
        public string? PreferredLocation { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    /// <summary>
    /// Query params for JobBank/Candidates/Search.
    /// </summary>
    public class JobBankCandidateSearchRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        public string? SearchText { get; set; }
        public int? RequisitionID { get; set; }

        public string? SkillsFilter { get; set; }  // comma-separated
        public int? MinExperienceYears { get; set; }
        public int? MaxExperienceYears { get; set; }
        public string? EducationKeyword { get; set; }
        public int? CityID { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Query params for JobBank/Candidates/List (admin list).
    /// </summary>
    public class JobBankCandidateListRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        public string? SearchText { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Response for Search and List: candidates + totalRecords.
    /// </summary>
    public class JobBankCandidateSearchResponseDto
    {
        public List<JobBankCandidateResponseDto> Candidates { get; set; } = new();
        public int TotalRecords { get; set; }
    }

    // =============================================
    // JOB BANK SHORTLIST DTOs
    // =============================================

    /// <summary>
    /// Response after shortlisting (POST JobBank/Shortlist).
    /// </summary>
    public class JobBankShortlistInsertResponseDto
    {
        public int JobBankShortlistID { get; set; }
    }

    /// <summary>
    /// Request to shortlist a job bank candidate for a requisition (POST JobBank/Shortlist).
    /// </summary>
    public class JobBankShortlistInsertRequestDto
    {
        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int RequisitionID { get; set; }

        [Required]
        public int JobBankCandidateID { get; set; }

        [StringLength(100)]
        public string? ShortlistedBy { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Shortlisted candidate item for a requisition (GET JobBank/Shortlist/ByRequisition/{id}).
    /// </summary>
    public class JobBankShortlistByRequisitionDto
    {
        public int JobBankShortlistID { get; set; }
        public int JobBankCandidateID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime? ShortlistedOn { get; set; }
        public string? ShortlistedBy { get; set; }
        public string? Remarks { get; set; }
    }
}
