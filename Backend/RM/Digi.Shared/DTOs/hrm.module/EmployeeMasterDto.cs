using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.hrm.module
{
    // Base DTO
    public class BaseDto
    {
        public int? EmployeeID { get; set; }
       // public bool IsActive { get; set; } = true;
       // public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }
    public class EmployeeCodeAuthDto
    {
        public string? EmployeeCode { get; set; }
        public string? CompanyName { get; set; }
        public string? EmployeeThumbnail { get; set; }
        public string? CompanyLogo { get; set; }
        public string? GeoFenceID { get; set; }
        public int? DepartmentID { get; set; }
    }
    // Main Employee DTO
    public class EmployeeMasterDto : BaseDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(100)]
        public string? FatherName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? CNIC { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CNICExpiryDate { get; set; }

        [StringLength(20)]
        public string? PassportNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PassportExpiryDate { get; set; }

        [StringLength(50)]
        public string? Nationalty { get; set; }

        [StringLength(50)]
        public string? NTN { get; set; }

        public string? Notes { get; set; }

        [StringLength(10)]
        public string? PostalCode { get; set; }

        public int? LineManagerID { get; set; }
        //public int? GeoFenceID { get; set; }
        public string? GeoFenceID { get; set; }

        [StringLength(250)]
        public string? PrimaryAddress { get; set; }

        [StringLength(250)]
        public string? SecondaryAddress { get; set; }

        [StringLength(20)]
        public string? PersonalPhone { get; set; }

        [StringLength(20)]
        public string? WorkPhone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? PersonalEmail { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? WorkEmail { get; set; }

        [StringLength(100)]
        public string? CompanyName { get; set; }
        [Required]
        public int CompanyID { get; set; }

        public int? DepartmentID { get; set; }
        public int? DesignationID { get; set; }
        public int? EmploymentStatusID { get; set; }
        public int? BloodGroupID { get; set; }
        public int? GenderID { get; set; }
        public int? ReligionID { get; set; }
        public int? MaritalStatusID { get; set; }
        public int? SalaryTypeID { get; set; }
        public int? WorkModeID { get; set; }
        public int? ShiftTypeID { get; set; }
        public int? SalaryStatusID { get; set; }
        public int? CurrencyID { get; set; }
        public decimal? PrimaryPercent { get; set; }
        public decimal? SecondaryPercent { get; set; }
        public decimal? amount { get; set; }
        public int? TaxStatusID { get; set; }
        public int? PaymentMethodID { get; set; }
        public int? CityID { get; set; }
        public int? StateProvinceID { get; set; }
        public int? CountryID { get; set; }
        public int? Grade { get; set; }
        public bool IsResigned { get; set; }
        public bool IsDeleted { get; set; }

        [DataType(DataType.Date)]
        public DateTime? JoiningDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ProbationDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ConfirmationDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ResignDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LastWorkingDay { get; set; }


        // Related data
        public List<HealthInsuranceDto>? HealthInsurances { get; set; }
        public List<NextOfKinDto>? NextOfKins { get; set; }
        public List<EmergencyContactDto>? EmergencyContacts { get; set; }
        public List<DisabilityDto>? Disabilities { get; set; }
        public List<ExperienceDto>? Experiences { get; set; }
        public List<QualificationDto>? Qualifications { get; set; }
        public List<EmployeePaymentMethodDto>? PaymentMethods { get; set; }

        [JsonIgnore]
        public List<AttachmentDto>? Attachments { get; set; }
        public List<AttachmentDocumentNameDto>? AttachmentDocumentName { get; set; }
    }

    // Related DTOs
    public class HealthInsuranceDto 
    {
        // public int EmployeeID { get; set; }
        // public int CompanyID { get; set; }

        public int? EmpRelatedID { get; set; } // For update scenarios

        [StringLength(100)]
        public string Provider { get; set; }

        [StringLength(50)]
        public string InsuranceNumber { get; set; }

        [StringLength(20)]
        public string CNIC { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CNICExpiryDate { get; set; }
    }

    public class NextOfKinDto 
    {
        //  public int EmployeeID { get; set; }
        //  public int CompanyID { get; set; }

        public int? NextOfKinID { get; set; } // For update scenarios

        [StringLength(100)]
        public string Name { get; set; }

        public int RelationTypeID { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DOB { get; set; }

        [StringLength(20)]
        public string CNIC { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CNICExpiryDate { get; set; }
        public bool? IsMedicalAllow { get; set; }
        public bool IsAlive { get; set; }
        public bool? IsNextOfKin { get; set; }
    }

    public class EmergencyContactDto
    {
        //  public int EmployeeID { get; set; }
        // public int CompanyID { get; set; }

        public int? EmergencyContactID { get; set; } // For update scenarios

        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(50)]
        public string Relation { get; set; }
    }

    public class DisabilityDto 
    {
        //   public int EmployeeID { get; set; }
        //  public int CompanyID { get; set; }

        public int? DisabilityID { get; set; } // For update scenarios

        [StringLength(100)]
        public string DisabilityName { get; set; }

        public string DisabilityDetails { get; set; }
    }

    public class ExperienceDto 
    {
        //  public int EmployeeID { get; set; }
        //  public int CompanyID { get; set; }
        public int? ExperienceID { get; set; } // For update scenarios
        public int? AttachmentId { get; set; }
        [JsonPropertyName("attachmentReference")]
        public string AttachmentReference { get; set; } // Frontend se file reference milega
        public int? AttachmentTempId { get; set; } // Frontend se attachment reference
        [StringLength(100)]
        public string CompanyName { get; set; }

        [StringLength(100)]
        public string JobTitle { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public string Responsibilities { get; set; }
        public bool IsCurrentJob { get; set; }
    }

    public class EmployeePaymentMethodDto
    {
        public int PaymentMethodID { get; set; }
        public string PaymentMethodType { get; set; }
        public int? BankID { get; set; }
        public int? BankBranchID { get; set; }
        public string BankAccountTitle { get; set; }
        public string BankAccountNo { get; set; }
        public string IBAN { get; set; }
        public string BranchCode { get; set; }
        public string WalletProvider { get; set; }
        public string WalletNumber { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime? EffectiveFrom { get; set; }
    }

    public class QualificationDto 
    {
        // public int EmployeeID { get; set; }
        // public int CompanyID { get; set; }
        public int? DocumentID { get; set; } // For existing documents
        public int? AttachmentId { get; set; }
        [JsonPropertyName("attachmentReference")]
        public string AttachmentReference { get; set; } // Frontend se file reference milega
        public int? AttachmentTempId { get; set; } // Frontend se attachment reference
        public int StatusQualificationID { get; set; }
        public int QualificationTypeID { get; set; }

        [StringLength(100)]
        public string DocumentName { get; set; }

        [StringLength(100)]
        public string Institution { get; set; }

        [StringLength(50)]
        public string RefrenceNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ValidityDate { get; set; }

        public int? CompletionYear { get; set; }

        [StringLength(20)]
        public string Grade { get; set; }
        public bool IsVerified { get; set; }
    }

    public class AttachmentDto
    {
        //  public int EmployeeID { get; set; }
        // public int? TempId { get; set; } // Frontend se unique ID ke liye

        public int? AttachmentDetailID { get; set; }
        public int? AttachmentTypeID { get; set; }
        public int? CompanyID { get; set; }
        public string? AttachmentType { get; set; }
        public string? DocumentName { get; set; }
        [StringLength(150)]
        public string FileName { get; set; }

        public string FilePath { get; set; }

        [StringLength(10)]
        public string? FileExtension { get; set; }

        public decimal? FileSizeKB { get; set; }

        [StringLength(100)]
        public string? FileHash { get; set; }
    }

    public class AttachmentDocumentNameDto
    {
        public int? AttachmentDetailID { get; set; }
        public string? AttachmentType { get; set; }
        public string? DocumentName { get; set; }
        public string? FileName { get; set; }
    }
    // Response DTO
    public class EmployeeCreationResponse
    {
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; }
        public string Message { get; set; }
    }

    public class EmployeeUpdateResponse
    {
        public int EmployeeID { get; set; }
        public string Message { get; set; }
        public string? AttachmentReference { get; set; } // For frontend to track attachments
        public int AttachmentsProcessed { get; set; }
    }
    public class EmployeeUpdateDto 
    {
        [Required(ErrorMessage = "Employee ID is required for update")]
        public int EmployeeID { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(100)]
        public string? FatherName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? CNIC { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CNICExpiryDate { get; set; }

        [StringLength(20)]
        public string? PassportNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PassportExpiryDate { get; set; }

        [StringLength(50)]
        public string? Nationalty { get; set; }

        [StringLength(50)]
        public string? NTN { get; set; }

        public string? Notes { get; set; }

        [StringLength(10)]
        public string? PostalCode { get; set; }

        public int? LineManagerID { get; set; }
        //public int? GeoFenceID { get; set; }
        public string? GeoFenceID { get; set; }

        [StringLength(250)]
        public string? PrimaryAddress { get; set; }

        [StringLength(250)]
        public string? SecondaryAddress { get; set; }

        [StringLength(20)]
        public string? PersonalPhone { get; set; }

        [StringLength(20)]
        public string? WorkPhone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? PersonalEmail { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? WorkEmail { get; set; }

        [StringLength(100)]
        public string? CompanyName { get; set; }
        public int? CompanyID { get; set; }

        public int? DepartmentID { get; set; }
        public int? AttachmentDetailID { get; set; }
        public int? DesignationID { get; set; }
        public int? EmploymentStatusID { get; set; }
        public int? BloodGroupID { get; set; }
        public int? GenderID { get; set; }
        public int? ReligionID { get; set; }
        public int? MaritalStatusID { get; set; }
        public int? SalaryTypeID { get; set; }
        public int? SalaryStatusID { get; set; }
        public int? CurrencyID { get; set; }
        public decimal? Grade { get; set; }
        public decimal? PrimaryPercent { get; set; }
        public decimal? SecondaryPercent { get; set; }
        public decimal? Amount { get; set; }
        public int? WorkModeID { get; set; }
        public int? ShiftTypeID { get; set; }
        public int? TaxStatusID { get; set; }
        public int? PaymentMethodID { get; set; }
        public int? CityID { get; set; }
        public int? StateProvinceID { get; set; }
        public int? CountryID { get; set; }

        public bool? IsResigned { get; set; }
        public bool? IsDeleted { get; set; }

        [DataType(DataType.Date)]
        public DateTime? JoiningDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ProbationDate { get; set; }
        public DateTime? ConfirmationDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ResignDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LastWorkingDay { get; set; }

        public string? UpdatedBy { get; set; } // User ID of the person updating
        public List<int>? DeletedAttachmentIds { get; set; }
        // Optional - Send only modified related data
        public List<HealthInsuranceDto>? HealthInsurances { get; set; }
        public List<NextOfKinDto>? NextOfKins { get; set; }
        public List<EmergencyContactDto>? EmergencyContacts { get; set; }
        public List<DisabilityDto>? Disabilities { get; set; }
        public List<ExperienceDto>? Experiences { get; set; }
        public List<QualificationDto>? Qualifications { get; set; }
        public List<UpdateEmployeePaymentMethodDto>? PaymentMethods { get; set; }

        public List<AttachmentUpdateDto>? Attachments { get; set; }
        public List<AttachmentDocumentNameDto>? AttachmentDocumentName { get; set; }
    }

    public class UpdateEmployeePaymentMethodDto
    {
        public int? EmployeePaymentMethodID { get; set; }
        public int PaymentMethodID { get; set; }
        public int? BankID { get; set; }
        public string? BankAccountTitle { get; set; }
        public string? BankAccountNo { get; set; }
        public string? IBAN { get; set; }
        public string PaymentMethodType { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime? EffectiveFrom { get; set; }
    }
    public class AttachmentUpdateDto
    {
        public int? AttachmentDetailID { get; set; }
        public int AttachmentTypeID { get; set; }
        public string AttachmentType { get; set; }
        public string DocumentName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileExtension { get; set; }
        public decimal FileSizeKB { get; set; }
        public string FileHash { get; set; }

        // For new uploads during update
        public bool IsNewUpload { get; set; }
        public string? TempFileId { get; set; }
    }
    public class EmployeeStatusUpdateDto
    {
        public int EmployeeID { get; set; }
        public int CompanyID { get; set; }
        public bool IsActive { get; set; }
        public int? EmploymentStatusID { get; set; }
        public bool? IsPayrollEnabled { get; set; }
        public bool? IsResigned { get; set; }
        public string? Remarks { get; set; }
        public DateTime? ResignDate { get; set; }
        public DateTime? LastWorkingDay { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class EmployeeImportDto
    {
        public int RowNumber { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FatherName { get; set; } = null!;
        public string? CNIC { get; set; }
        public DateTime? JoiningDate { get; set; }
        public decimal? Amount { get; set; }
    }

    public class EmployeeBulkImportRequestDto
    {
        public int CompanyID { get; set; }
        public List<EmployeeImportDto> Employees { get; set; } = new();
        public string? CreatedBy { get; set; }
    }

    public class EmployeeImportProcessResult
    {
        public int SuccessCount { get; set; }
        public int SkippedCount { get; set; }
        public List<EmployeeImportFailureDto> FailedEmployees { get; set; } = new();
    }

    public class EmployeeImportFailureDto
    {
        public int RowNumber { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Reason { get; set; } = null!;
    }

    public class EmployeeInsertResult
    {
        public int Status { get; set; }     // 1 = inserted, 0 = duplicate
        public string Reason { get; set; } = null!;
    }

    
    public class SubordinateInfoDto
    {
        public int Count { get; set; }
        public List<EmployeeListDto> Subordinates { get; set; } = new();
    }

    public class SubordinateDetailDto
    {
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int DepartmentID { get; set; }
        public string DesignationName { get; set; } = string.Empty;
    }

    public class SubordinateDepartmentDto
    {
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class SubordinateDataDto
    {
        public List<SubordinateDepartmentDto> Departments { get; set; } = new();
        public List<SubordinateDetailDto> Subordinates { get; set; } = new();
    }

    public class BulkUpdateLineManagerRequest
    {
        public List<int> EmployeeIDs { get; set; } = new();
        public int NewLineManagerID { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public int CompanyID { get; set; }
    }



    // ── Request DTO ─────────────────────────────────────────────────────
    public class CreateEmployeeTransferDto
    {
        public int EmployeeID { get; set; }
        public int CompanyID { get; set; }

        public string? NewDivision { get; set; }
        public int? NewDepartmentID { get; set; }
        public int? NewDesignationID { get; set; }
        public int? NewLineManagerID { get; set; }
        public int? NewLocationID { get; set; }
        public string? NewCostCenter { get; set; }
        public string? NewJobDescription { get; set; }

        public DateTime EffectiveDate { get; set; }
        public string? Justification { get; set; }
        public int? ReasonID { get; set; }
        public string? ReasonDescription { get; set; }

        public IFormFile? AttachmentFile { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    // ── Response DTO ─────────────────────────────────────────────────────
    public class EmployeeTransferDto
    {
        public int TransferID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? ProfilePic { get; set; }

        public string? CurrentDivision { get; set; }
        public string? CurrentDepartment { get; set; }
        public string? CurrentDesignation { get; set; }
        public string? CurrentManager { get; set; }

        public string? NewDivision { get; set; }
        public string? NewDepartment { get; set; }
        public string? NewDesignation { get; set; }
        public string? NewManager { get; set; }
        public string? NewCostCenter { get; set; }
        public string? NewJobDescription { get; set; }

        public DateTime EffectiveDate { get; set; }
        public string? Justification { get; set; }
        public string? ReasonDescription { get; set; }
        public string? AttachmentPath { get; set; }
        public string? AttachmentFileName { get; set; }

        // ✅ String instead of int
        public string ApprovalStatus { get; set; } = "Pending";
        public bool IsApplied { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
    }


    // ── Request DTO ──────────────────────────────────────────────────────
    public class CreateInquiryDto
    {
        public int EmployeeID { get; set; }

        public int UserID { get; set; }
        public int CompanyID { get; set; }
        public int? InquiryTypeID { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class ResolveInquiryDto
    {
        public int InquiryID { get; set; }
        public string? ResolvedBy { get; set; }
        public string? ResolvedRemarks { get; set; }
        public string Status { get; set; } = "Approved";
    }

    // ── Response DTO ─────────────────────────────────────────────────────
    public class InquiryDto
    {
        public int InquiryID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? ProfilePic { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public int? InquiryTypeID { get; set; }
        public string? InquiryTypeName { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";
        public bool IsResolved { get; set; }
        public string? ResolvedBy { get; set; }
        public DateTime? ResolvedOn { get; set; }
        public string? ResolvedRemarks { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class InquiryEmployeeDto
    {
        public int CompanyID { get; set; }
        public string EmployeeName { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
    }


    public class InquiryStatusHistoryDto
    {
        public int HistoryID { get; set; }
        public int InquiryID { get; set; }
        public string? FromStatus { get; set; }
        public string ToStatus { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public string? ChangedByName { get; set; }
        public string? Remarks { get; set; }
        public DateTime ChangedOn { get; set; }
    }

    public class UpdateInquiryStatusDto
    {
        public int InquiryID { get; set; }
        public string ToStatus { get; set; } = string.Empty;
        public string? Remarks { get; set; }

        // Set server-side from the current user in the controller —
        // never trust a value the client sends for this.
        public string ChangedBy { get; set; } = string.Empty;
    }


    // ── Request DTO ────────────────────────────────────────────────────────
    public class CreateAdvanceSalaryConfigDto
    {
        public int CompanyID { get; set; }
        public string SalaryBasis { get; set; } = string.Empty;
        public decimal MaxPercentage { get; set; }
        public bool PermanentEmployeeOnly { get; set; }
        public int ApplyFromDay { get; set; }
        public int ApplyToDay { get; set; }
        public int ReapplyAfterMonths { get; set; }
        // ✅ RepaymentMonths REMOVED
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class UpdateAdvanceSalaryConfigDto
    {
        public int ConfigID { get; set; }
        public int CompanyID { get; set; }
        public string SalaryBasis { get; set; } = string.Empty;
        public decimal MaxPercentage { get; set; }
        public bool PermanentEmployeeOnly { get; set; }
        public int ApplyFromDay { get; set; }
        public int ApplyToDay { get; set; }
        public int ReapplyAfterMonths { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    // ── Response DTO ───────────────────────────────────────────────────────
    public class AdvanceSalaryConfigDto
    {
        public int ConfigID { get; set; }
        public int CompanyID { get; set; }
        public string SalaryBasis { get; set; } = string.Empty;
        public decimal MaxPercentage { get; set; }
        public bool PermanentEmployeeOnly { get; set; }
        public int ApplyFromDay { get; set; }
        public int ApplyToDay { get; set; }
        public int ReapplyAfterMonths { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }



    // ── Request: Create ───────────────────────────────────────────────────
    public class CreateLoanConfigDto
    {
        public int CompanyID { get; set; }
        public bool PermanentEmployeeOnly { get; set; }
        public int MinServiceMonths { get; set; }
        public int NoOfGrossSalaries { get; set; }
        public string MaxLimitType { get; set; } = "fixed"; // 'fixed' | 'annual'
        public decimal? MaxLimitAmount { get; set; }
        public decimal? MaxLimitPercentage { get; set; }
        public int MaxInstallmentMonths { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    // ── Request: Update ───────────────────────────────────────────────────
    public class UpdateLoanConfigDto
    {
        public int ConfigID { get; set; }
        public int CompanyID { get; set; }
        public bool PermanentEmployeeOnly { get; set; }
        public int MinServiceMonths { get; set; }
        public int NoOfGrossSalaries { get; set; }
        public string MaxLimitType { get; set; } = "fixed";
        public decimal? MaxLimitAmount { get; set; }
        public decimal? MaxLimitPercentage { get; set; }
        public int MaxInstallmentMonths { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    // ── Response ──────────────────────────────────────────────────────────
    public class LoanConfigDto
    {
        public int ConfigID { get; set; }
        public int CompanyID { get; set; }
        public bool PermanentEmployeeOnly { get; set; }
        public int MinServiceMonths { get; set; }
        public int NoOfGrossSalaries { get; set; }
        public string MaxLimitType { get; set; } = string.Empty;
        public decimal? MaxLimitAmount { get; set; }
        public decimal? MaxLimitPercentage { get; set; }
        public int MaxInstallmentMonths { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }


    // ── Apply Info (returned on page load) ────────────────────────────────
    public class AdvanceSalaryApplyInfoDto
    {
        // Config
        public int ConfigID { get; set; }
        public string SalaryBasis { get; set; } = string.Empty;
        public decimal MaxPercentage { get; set; }
        public bool PermanentEmployeeOnly { get; set; }
        public int ApplyFromDay { get; set; }
        public int ApplyToDay { get; set; }
        public int ReapplyAfterMonths { get; set; }

        // Employee
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public DateTime JoiningDate { get; set; }
        public string? EmploymentStatus { get; set; }
        public string? ProfilePic { get; set; }
        public DateTime? LastApprovedOn { get; set; }

        // Computed
        public decimal MaxAllowedAmount => BasicSalary * MaxPercentage / 100;
    }

    // ── Create Request ────────────────────────────────────────────────────
    public class CreateAdvanceSalaryRequestDto
    {
        public int EmployeeID { get; set; }
        public int CompanyID { get; set; }
        public int ConfigID { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal RequestAmount { get; set; }
        public string AmountInWords { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    // ── Response ──────────────────────────────────────────────────────────
    public class AdvanceSalaryRequestDto
    {
        public int RequestID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal RequestAmount { get; set; }
        public string AmountInWords { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";
        public bool IsApplied { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string? RejectedBy { get; set; }
        public DateTime? RejectedOn { get; set; }
        public string? RejectionReason { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
    }



    // ── Returned by sp_Hr_GetLoanApplyInfo (ResultSet 1) ──────────────────
    public class LoanApplyConfigDto
    {
        public int ConfigID { get; set; }
        public int CompanyID { get; set; }
        public bool PermanentEmployeeOnly { get; set; }
        public int MinServiceMonths { get; set; }
        public int NoOfGrossSalaries { get; set; }
        public string MaxLimitType { get; set; } = string.Empty; // "fixed" | "annual"
        public decimal? MaxLimitAmount { get; set; }
        public decimal? MaxLimitPercentage { get; set; }
        public int MaxInstallmentMonths { get; set; }
        public bool IsActive { get; set; }
    }

    // ── Returned by sp_Hr_GetLoanApplyInfo (ResultSet 2) ──────────────────
    public class LoanApplyEmployeeDto
    {
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string DesignationName { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal AnnualSalary { get; set; }
        public DateTime JoiningDate { get; set; }
        public int ServiceMonths { get; set; }
        public string EmploymentStatus { get; set; } = string.Empty;
        public bool IsPermanent { get; set; }
        public string? ProfilePic { get; set; }
        public bool HasActiveLoan { get; set; }
        public DateTime? LastApprovedOn { get; set; }
    }

    // ── Combined response for GetApplyInfo ────────────────────────────────
    public class LoanApplyInfoResponseDto
    {
        public LoanApplyConfigDto? Config { get; set; }
        public LoanApplyEmployeeDto? Employee { get; set; }
    }

    // ── Create request DTO (from Angular) ─────────────────────────────────
    public class CreateLoanRequestDto
    {
        // Set by controller from JWT — not from client
        public int EmployeeID { get; set; }
        public int CompanyID { get; set; }
        public string? companyName { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // From Angular form
        public int ConfigID { get; set; }
        public decimal RequestedAmount { get; set; }
        public int InstallmentMonths { get; set; }
        public string Reason { get; set; } = string.Empty;

        // File path — set by controller after saving file
        public string? AttachmentPath { get; set; }
    }

    // ── SP result (IsSuccess + Message + RequestID) ───────────────────────
    public class LoanRequestResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? RequestID { get; set; }
    }

    // ── List row DTO ──────────────────────────────────────────────────────
    public class LoanRequestDto
    {
        public int RequestID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string DesignationName { get; set; } = string.Empty;
        public decimal RequestedAmount { get; set; }
        public decimal GrossSalary { get; set; }
        public int InstallmentMonths { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? AttachmentPath { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public bool IsApplied { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string? RejectedBy { get; set; }
        public DateTime? RejectedOn { get; set; }
        public string? RejectionReason { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
    }


    // ── Approve request (from Angular) ────────────────────────────────────
    public class ApproveLoanRequestDto
    {
        public int RequestID { get; set; }
        // Set by controller from JWT
        public int CompanyID { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        // From form
        public DateTime StartDate { get; set; }
    }

    // ── Reject request (from Angular) ─────────────────────────────────────
    public class RejectLoanRequestDto
    {
        public int RequestID { get; set; }
        public int CompanyID { get; set; }
        public string RejectedBy { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
    }

    // ── SP result (shared: IsSuccess + Message) ───────────────────────────
    public class LoanActionResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    
    // ── Installment log row ───────────────────────────────────────────────
    public class LoanInstallmentLogDto
    {
        public int LogID { get; set; }
        public int RequestID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int InstallmentNo { get; set; }
        public DateTime DueDate { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public string Status { get; set; } = string.Empty; // Pending | Paid | Skipped
        public string? PayrollMonth { get; set; }
        public string? Remarks { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    // ── Mark installment paid (from payroll) ──────────────────────────────
    public class MarkInstallmentPaidDto
    {
        public int LogID { get; set; }
        public int CompanyID { get; set; }
        public decimal PaidAmount { get; set; }
        public string PayrollMonth { get; set; } = string.Empty; // 'YYYY-MM'
        public string? Remarks { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }


    public class EmployeeShiftDto
    {
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? JoiningDate { get; set; }
        public int ShiftTypeID { get; set; }
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string DesignationName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
    }

    public class BulkShiftUpdateRequestDto
    {
        public List<int> EmployeeIDs { get; set; } = new();
        public int NewShiftTypeID { get; set; }
        public int CompanyID { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class ShiftEmployeesResultDto
    {
        public List<EmployeeShiftDto> Employees { get; set; } = new();
        public List<ShiftDepartmentDto> Departments { get; set; } = new();
    }

    public class ShiftDepartmentDto
    {
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }
}