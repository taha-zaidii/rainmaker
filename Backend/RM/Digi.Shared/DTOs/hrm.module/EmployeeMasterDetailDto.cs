using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.hrm.module
{
    public class EmployeeListDto
    {
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public DateTime JoiningDate { get; set; }
        public string DepartmentName { get; set; }
        public string DesignationName { get; set; }
        public string? FilePath { get; set; }
    }

    public class EmployeeLineManagerDto
    {
        public int EmployeeID { get; set; }
        public string? EmployeeCode { get; set; }
        public int? LineManagerID { get; set; }
        public string? LineManagerName { get; set; }
        public int? DesignationID { get; set; }
        public string? DesignationName { get; set; }
        public string? workEmail { get; set; }
        public string? workPhone { get; set; }

        public string? FilePath { get; set; }
        public int? BloodGroupID { get; set; }
        public string? BloodGroupName { get; set; }
    }
    public class EmployeeFilterDto
    {
        public int? EmployeeID { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? JoiningDate { get; set; }
        public int? DepartmentID { get; set; }
        public int? DesignationID { get; set; }
        public int? GenderID { get; set; }
        public int? ReligionID { get; set; }
        public int? CityID { get; set; }
        public int? CountryID { get; set; }
        public int? CurrencyID { get; set; }
        public int? Grade { get; set; }
        public int? LocationID { get; set; }
        public int? ShiftTypeID { get; set; }
        public int? LineManagerID { get; set; }
        public int? PaymentMethodID { get; set; }
        public int? WorkModeID { get; set; }
        public bool? IsActive { get; set; } = true;
        public bool? IsResigned { get; set; }
        //public bool? IsDeleted { get; set; } = false;
    }
    public class EmployeeMasterDetailsDto
    {
        public int EmployeeID { get; set; }
        public int? AttachmentDetailID { get; set; }

        public string? EmployeeCode { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FatherName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? CNIC { get; set; }
        public DateTime? CNICExpiryDate { get; set; }
        public string? PassportNumber { get; set; }
        public DateTime? PassportExpiryDate { get; set; }
        public string? Nationalty { get; set; }
        public string? NTN { get; set; }
        public string? Notes { get; set; }
        public string? PostalCode { get; set; }

        public int? LineManagerID { get; set; }
        public string? LineManagerName { get; set; }
       // public int? GeoFenceID { get; set; }
        public string? GeoLocationName { get; set; }
        
        public string? GeoFenceID { get; set; }         
        public List<int>? GeoFenceIDs { get; set; }     
        
        public string? PrimaryAddress { get; set; }
        public string? SecondaryAddress { get; set; }
        public string? PersonalPhone { get; set; }
        public string? WorkPhone { get; set; }
        public string? PersonalEmail { get; set; }
        public string? WorkEmail { get; set; }

        public int? CompanyID { get; set; }
        public string? CompanyName { get; set; }

        public int? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }

        public int? DesignationID { get; set; }
        public string? DesignationName { get; set; }

        public int? EmploymentStatusID { get; set; }
        public string? EmploymentStatus { get; set; }

        public int? BloodGroupID { get; set; }
        public string? BloodGroupName { get; set; }

        public int? GenderID { get; set; }
        public string? GenderName { get; set; }

        public int? ReligionID { get; set; }
        public string? ReligionName { get; set; }

        public int? MaritalStatusID { get; set; }
        public string? MaritalStatusName { get; set; }
        public int? CurrencyID { get; set; }
        public string? CurrencyCode { get; set; }

        public string? GradeName { get; set; }
        public int? Grade { get; set; }
        public int? SalaryTypeID { get; set; }
        public string? SalaryTypeName { get; set; }
        public decimal? Amount { get; set; }

        public int? WorkModeID { get; set; }
        public string? WorkModeName { get; set; }

        public int? ShiftTypeID { get; set; }
        public string? ShiftTypeName { get; set; }

        public int? TaxStatusID { get; set; }
        public string? TaxStatusName { get; set; }

        public int? SalaryStatusID { get; set; }
        public string? SalaryStatusName { get; set; }
        public decimal? PrimaryPercent { get; set; }
        public decimal? SecondaryPercent { get; set; }
        public int? PaymentMethodID { get; set; }
        public string? PaymentMethodName { get; set; }

        public int? CityID { get; set; }
        public string? CityName { get; set; }

        public int? StateProvinceID { get; set; }
        public string? StateName { get; set; }

        public int? CountryID { get; set; }
        public string? CountryName { get; set; }

        public bool IsActive { get; set; }
        public bool IsResigned { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPayrollEnabled { get; set; }

        public DateTime? JoiningDate { get; set; }
        public DateTime? ProbationDate { get; set; }
        
        public DateTime? ResignDate { get; set; }
        public DateTime? Last_Working_Day { get; set; }
        public DateTime? ConfirmationDate { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public int? UserID { get; set; }
        public int? RoleID { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public bool? UserIsActive { get; set; }
        public List<EmployeeAttachmentDto> Attachments { get; set; }
        public List<EmployeeDisabilityDto> Disabilities { get; set; }
        public List<EmployeeEmergencyContactDto> EmergencyContacts { get; set; }
        public List<EmployeeExperienceDto> Experiences { get; set; }
        public List<EmployeeHealthInsuranceDto> HealthInsurances { get; set; }
        public List<EmployeeNextOfKinDto> NextOfKins { get; set; }
        public List<EmployeeQualificationDto> Qualifications { get; set; }
        public List<EmployeeGetPaymentMethodDto>? PaymentMethods { get; set; }
    }


    public class EmployeeAttachmentDto
    {
        public int AttachmentDetailID { get; set; }
        public int AttachmentTypeID { get; set; }
        public string AttachmentType { get; set; }
        public string DocumentName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileExtension { get; set; }
        public decimal? FileSizeKB { get; set; }
        public string FileHash { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool IsActive { get; set; }
    }
    public class EmployeeDisabilityDto
    {
        public int DisabilityID { get; set; }
        public string DisabilityName { get; set; }
        public string DisabilityDetails { get; set; }
        public int EmployeeID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
    public class EmployeeEmergencyContactDto
    {
        public int EmergencyContactID { get; set; }
        public int EmployeeID { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Relation { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
    public class EmployeeGetPaymentMethodDto
    {
        public int EmployeePaymentMethodID { get; set; }
        public int EmployeeID { get; set; }
        public int PaymentMethodID { get; set; }
        public string PaymentMethodType { get; set; }

        public int? BankID { get; set; }
        public string BankName { get; set; }

        public string BankAccountTitle { get; set; }
        public string BankAccountNo { get; set; }
        public string IBAN { get; set; }

        public string WalletProvider { get; set; }
        public string WalletNumber { get; set; }

        public bool IsPrimary { get; set; }
        public DateTime? EffectiveFrom { get; set; }
    }

    public class EmployeeExperienceDto
    {
        public int ExperienceID { get; set; }
        public int EmployeeID { get; set; }
        public string CompanyName { get; set; }
        public string JobTitle { get; set; }
        public bool IsCurrentJob { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Responsibilities { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool IsActive { get; set; }
    }
    public class EmployeeHealthInsuranceDto
    {
        public int EmpRelatedID { get; set; }
        public int EmployeeID { get; set; }
        public string Provider { get; set; }
        public string InsuranceNumber { get; set; }
        public string CNIC { get; set; }
        public DateTime? CNICExpiryDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
    public class EmployeeNextOfKinDto
    {
        public int NextOfKinID { get; set; }
        public int EmployeeID { get; set; }
        public string Name { get; set; }
        public int RelationTypeID { get; set; }
        public string RelationType { get; set; }
        public string Phone { get; set; }
        public DateTime? DOB { get; set; }
        public string CNIC { get; set; }
        public DateTime? CNICExpiryDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsMedicalAllow { get; set; }
        public bool? IsNextOfkin { get; set; }
        public bool? IsAlive { get; set; }
    }
    public class EmployeeQualificationDto
    {
        public int DocumentID { get; set; }
        public int EmployeeID { get; set; }

        public int? StatusQualificationID { get; set; }
        public string QualificationStatus { get; set; }

        public int? QualificationTypeID { get; set; }
        public string QualificationType { get; set; }

        public string DocumentName { get; set; }
        public string Institution { get; set; }
        public string RefrenceNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ValidityDate { get; set; }
        public string CompletionYear { get; set; }
        public string Grade { get; set; }
        public bool IsVerified { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool IsActive { get; set; }
    }



}
