namespace Digi.Shared.DTOs.admin.module
{
    public class UsersResponseDto
    {
        public int UserID { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public int? EmployeeID { get; set; }
        public int? RoleID { get; set; }
        public string? RoleName { get; set; }
        public int? CompanyID { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? LastActivityOn { get; set; }
        public bool? IsGlobalAdmin { get; set; }
        public bool? IsDeleted { get; set; }
        //public bool? LockoutEnabled { get; set; }
        public bool? EmailConfirmed { get; set; }
        public bool? IsPayrollEnabled { get; set; }
        //public bool? MustChangePassword { get; set; }
        // public bool? IsLockedOut { get; set; }
        //public bool? TwoFactorEnabled { get; set; }
        //public bool? InheritCompanyTheme { get; set; }
        // public int? CustomThemeID { get; set; }
    }

    public class UserCreateDto
    {
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PasswordHash { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public int? EmployeeID { get; set; }
        public int? CompanyID { get; set; }
        public int? RoleID { get; set; }
        public bool? IsActive { get; set; } = true;
        public string? CreatedBy { get; set; }
       // public bool? EmailConfirmed { get; set; } = false;
       // public bool? MustChangePassword { get; set; } = false;
       // public bool? LockoutEnabled { get; set; } = true;
       // public bool? TwoFactorEnabled { get; set; } = false;
       // public bool? InheritCompanyTheme { get; set; } = true;
       // public int? CustomThemeID { get; set; }
    }

    public class UserUpdateDto
    {
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public int? EmployeeID { get; set; }
        public int? RoleID { get; set; }
        public int? CompanyID { get; set; }
        public bool? IsActive { get; set; }
        public string? UpdatedBy { get; set; }
        public bool? SendEmailOnEmailChange { get; set; }
        public bool? NotifyManagersOnUpdate { get; set; }
       // public bool? EmailConfirmed { get; set; }
       // public bool? MustChangePassword { get; set; }
       // public bool? LockoutEnabled { get; set; }
       // public bool? TwoFactorEnabled { get; set; }
       // public bool? InheritCompanyTheme { get; set; }
       // public int? CustomThemeID { get; set; }
    }
    public class UserVerificationDto
    {
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? UserID { get; set; }
        public int? RoleID { get; set; }
        public string? SecurityStamp { get; set; }
        public string? PasswordHash { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? EmployeeID { get; set; }
        public int? CompanyID { get; set; }
        public bool? IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; } = true;
        public string? CreatedBy { get; set; }
        public bool? EmailConfirmed { get; set; } = false;
        public bool? IsGlobalAdmin { get; set; }
        public object CreatedOn { get; set; }
        // public bool? MustChangePassword { get; set; } = false;
        // public bool? LockoutEnabled { get; set; } = true;
        // public bool? TwoFactorEnabled { get; set; } = false;
        // public bool? InheritCompanyTheme { get; set; } = true;
        // public int? CustomThemeID { get; set; }
    }

    public class UserLookupResultDto
    {
        public UserVerificationDto? User { get; set; }
        public bool HasMultipleMatches { get; set; }
        public string? Message { get; set; }
        public bool IsSuccess => User != null;
    }

    public class DisablePayrollRequest
    {
        public int? UserID { get; set; }
        public int? EmployeeID { get; set; }
        public bool IsPayrollEnabled { get; set; } // supplied by client, but backend enforces FALSE per spec
        public int CompanyID { get; set; }
    }

    public class DisablePayrollResult
    {
        public int? UserID { get; set; }
        public int? EmployeeID { get; set; }
        public bool? IsPayrollEnabled { get; set; }
        public bool? IsActiveUser { get; set; }
        public string? Message { get; set; } = "";
        public bool? IsSuccess { get; set; }   // NEW
    }

    //public class UserRegistrationDto
    //{
    //    public string? UserName { get; set; }

    //    public string? FullName { get; set; }

    //    public string? PasswordHash { get; set; }

    //    public string? Mobile { get; set; }

    //    public string? Email { get; set; }

    //    public string? EmployeeID { get; set; }

    //    public int? CompanyID { get; set; }
    //    public int? ModuleID { get; set; }
    //    public int? RoleID { get; set; }

    //    public bool? IsActive { get; set; }

    //    public DateTime? CreatedOn { get; set; }

    //    public string? CreatedBy { get; set; }

    //    public DateTime? UpdatedOn { get; set; }

    //    public string? UpdatedBy { get; set; }
    //}
}
