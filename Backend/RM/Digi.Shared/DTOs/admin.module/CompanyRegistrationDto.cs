namespace Digi.Shared.DTOs.admin.module
{
    //public class CompanyRegistrationDto
    //{
    //    // Company Info
    //    public string CompanyName { get; set; }
    //    public string CompanyShortName { get; set; }
    //    public string CompanyEmail { get; set; }
    //    public string CompanyPhone { get; set; }
    //    public string CompanyMobile { get; set; }
    //    public int CountryID { get; set; }
    //    public int CityID { get; set; }
    //    public string PostalCode { get; set; }
    //    public string Address { get; set; }

    //    public string CreatedBy { get; set; }

    //    // User Info
    //    public string UserSecurityStamp { get; set; }
    //    public string UserFullName { get; set; }
    //    public string UserName { get; set; }
    //    public string UserEmail { get; set; }
    //    public string UserMobile { get; set; }
    //    public string PasswordHash { get; set; }

    //    // Subscription Info
    //    public DateTime SubscriptionStartDate { get; set; }
    //    public DateTime SubscriptionEndDate { get; set; }
    //    public int PackageID { get; set; }

    //    // Module & Navigation Info
    //    public string SelectedModuleIDs { get; set; } // e.g., "1,2,3"
    //    public int MaxUsers { get; set; }
    //    public string SelectedNavIDs { get; set; } // e.g., "10,11,12"

    //    public string SelectedNavPermissions { get; set; } // JSON string
    //}

    #region Registration DTOs
    public class CompanyRegistrationDto
    {
        public string CompanyName { get; set; }
        public string CompanyShortName { get; set; }
        public string CompanyImageUrl { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyMobile { get; set; }
        public int CountryID { get; set; }
        public int CityID { get; set; }
        public int StateID { get; set; }
        public string Address { get; set; }
        public string CreatedBy { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserMobile { get; set; }
        public int? PackageID { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }

    public class CompanyRegistrationResult
    {
        public bool Success { get; set; }
        public int CompanyID { get; set; }
        public string Message { get; set; }
    }
    #endregion

    #region Company DTOs
    public class CompanyDetailsDto
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string CompanyShortName { get; set; }
        public string CompanyLogo { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyMobile { get; set; }
        public int CountryID { get; set; }
        public int CityID { get; set; }
        public int StateID { get; set; }
        public string Address { get; set; }
        public int OwnerID { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; }
        public bool IsVerified { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
        public string OwnerUserName { get; set; }
        public string OwnerEmail { get; set; }
        public string OwnerMobile { get; set; }
        public int? SubscriptionID { get; set; }
        public int? PackageID { get; set; }
        public string PackageName { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public bool? SubscriptionIsActive { get; set; }
        public int? AdminRoleID { get; set; }
        public string AdminRoleName { get; set; }
        public List<CompanyFeatureDto> Features { get; set; } = new();
    }

    public class CompanyFeatureDto
    {
        public int CompanyID { get; set; }
        public int RoleId { get; set; }
        public int FeatureID { get; set; }
        public int PackageID { get; set; }
        public int ModuleID { get; set; }
        public string ModuleName { get; set; }
        public int NavID { get; set; }
        public string NavName { get; set; }
        public int? ParentID { get; set; }
    }

    public class CompanyUpdatesDto
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string CompanyShortName { get; set; }
        public string CompanyImageUrl { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyMobile { get; set; }
        public int CountryID { get; set; }
        public int CityID { get; set; }
        public int StateID { get; set; }
        public string Address { get; set; }
        public string UpdatedBy { get; set; }
        public int? PackageID { get; set; }
    }

    #endregion

}
