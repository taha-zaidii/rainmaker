using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Digi.Shared.DTOs.admin.module
{
    //public class CompanyCreateDto
    //{
    //    public string? CompanyName { get; set; }
    //    public string? ShortName { get; set; }
    //    public string? Address { get; set; }
    //    public int? OwnerID { get; set; }
    //    public int? CountryID { get; set; }
    //    public int? CityID { get; set; }
    //    public string? PostalCode { get; set; }
    //    public string? Phone { get; set; }
    //    public string? Mobile { get; set; }
    //    public string? Email { get; set; }
    //    public string? Website { get; set; }
    //    public int? DefaultCurrencyID { get; set; }
    //    public bool IsActive { get; set; }
    //    public string? CreatedBy { get; set; }
    //}
    //public class CompanyUpdateDto
    //{
    //    public string? CompanyName { get; set; }
    //    public string? ShortName { get; set; }
    //    public string? Address { get; set; }
    //    public int? OwnerID { get; set; }
    //    public int? CountryID { get; set; }
    //    public int? CityID { get; set; }
    //    public string? PostalCode { get; set; }
    //    public string? Phone { get; set; }
    //    public string? Mobile { get; set; }
    //    public string? Email { get; set; }
    //    public string? Website { get; set; }
    //    public bool IsActive { get; set; }
    //    public string? UpdatedBy { get; set; }
    //}
    public class CompanyDto
    {
        public int CompanyID { get; set; }
        public string? CompanyName { get; set; }
        public string? ShortName { get; set; }
        public string? Address { get; set; }
        public int? OwnerID { get; set; }
        public int? CountryID { get; set; }
        public int? CityID { get; set; }
       // public string? PostalCode { get; set; }

        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public int? DefaultCurrencyID { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? IsDeletedOn { get; set; }
        public bool IsDeleted { get; set; }
    }
    public class CompanyUpdateDto
    {
        public int CompanyID { get; set; }
        [Required]
        public string CompanyName { get; set; }
        public int? AttachmentDetailID { get; set; }
        public string? CompanyShortName { get; set; }
        public string? CompanyImageUrl { get; set; }
        public IFormFile? AttachmentFile { get; set; }
        public string? AttachmentURL { get; set; }
        [Required]
        [EmailAddress]
        public string? CompanyEmail { get; set; }
        public string? CompanyPhone { get; set; }
        public string? CompanyMobile { get; set; }
        public int? CountryID { get; set; }
        public int? CityID { get; set; }
        public int? StateID { get; set; }
        // public string? PostalCode { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? UpdatedBy { get; set; }

        // User Info
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }
        public string? UserName { get; set; }

        [Required]
        [EmailAddress]
        public string? UserEmail { get; set; }

        public string? UserMobile { get; set; }

        //[Required]
        //public string? PasswordHash { get; set; }

        // Subscription Info
        public int? PackageID { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }

    }
    public class CompanyCreateDto
    {
      
        [Required]
        public string? CompanyName { get; set; }

        public string? CompanyShortName { get; set; }
        public string? CompanyImageUrl  { get; set; }
        public IFormFile? AttachmentFile { get; set; }
        public string? AttachmentURL { get; set; }

        [Required]
        [EmailAddress]
        public string? CompanyEmail { get; set; }

        public string? CompanyPhone { get; set; }
        public string? CompanyMobile { get; set; }
        public int? CountryID { get; set; }
        public int? CityID { get; set; }
        public int? StateID { get; set; }
       // public string? PostalCode { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }

        [Required]
        public string? CreatedBy { get; set; }

        // User Info
        public string? FirstName { get; set; }
        public string?   LastName { get; set; }
        public string? UserName { get; set; }

        [Required]
        [EmailAddress]
        public string? UserEmail { get; set; }

        public string? UserMobile { get; set; }

        //[Required]
        //public string PasswordHash { get; set; }

        // Subscription Info
        public int? PackageID { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        //public int? PackageRoleID { get; set;}
       // public List<int> SelectedFeatureIDs { get; set; } = new List<int>();
    }
    public class CompanyResponseDto
    {
        public int? CompanyID { get; set; }
        public int? AttachmentDetailID { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyShortName { get; set; }
        public string? CompanyImageUrl { get; set; }
        public string? CompanyEmail { get; set; }
        public string? CompanyPhone { get; set; }
        public string? CompanyMobile { get; set; }
        public int? CountryID { get; set; }
        public string? CountryName { get; set; }
        public int? CityID { get; set; }
        public string? CityName { get; set; }
        public int? StateID { get; set; }
        public string? StateName { get; set; }
        public string? Address { get; set; }
        public int? OwnerID { get; set; }
        public string? Status { get; set; }
        public string? CompanyLogo { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public string? Website { get; set; }
        // Owner Info
        public string? OwnerUserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? OwnerEmail { get; set; }
        public string? OwnerMobile { get; set; }


        // role info
        public string? AdminRoleName { get; set; }

        // Subscription Info
        public int? SubscriptionID { get; set; }
        public int? PackageID { get; set; }
        public string? PackageName { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public string? SubscriptionStatus { get; set; }
        public int? ModuleID { get; set; }
        public string? ModuleName { get; set; }
        // Features
       // public List<CompanyFeatureDto> Features { get; set; } = new ();
    }

    //public class CompanyFeatureDto
    //{
    //    public int? CompanyID { get; set; }
    //    public int? FeatureID { get; set; }
    //    public int? ModuleID { get; set; }
    //    public string? ModuleName { get; set; }
    //    public int? NavID { get; set; } 
    //    public string? NavName { get; set; }
    //    public int? ParentID { get; set; }

    //}

    public class VerifyCompanyDto
    {
        public string? CompanyID { get; set; }
        public string? OTP { get; set; }
        public string? NewPassword { get; set; } // Optional
    }

    public class OtpVerificationResult
    {
        public bool IsVerified { get; set; }
        public bool IsLocked { get; set; }
        public int RemainingAttempts { get; set; }
        public bool IsPasswordUpdated { get; set; }
    }

    // Supporting classes
    public class ResendOtpDto
    {
        public string? CompanyID { get; set; }
        //public string IPAddress { get; set; } // Optional for logging
    }

    public class ResendOtpResult
    {
        public string? OTP { get; set; }
        public string? CompanyEmail { get; set; }
        public string? UserEmail { get; set; }
        public string? VerificationLink { get; set; }
    }
}


