namespace Digi.Shared.DTOs.admin.module
{
    public class UserUpdateDto
    {
        public int UserID { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? EmployeeID { get; set; }
        public int? CompanyID { get; set; }
        public string? ModuleID { get; set; }
        //public int[]? ModuleID { get; set; }
        public string? RoleIDs { get; set; }
        public string? ModifiedBy { get; set; }
    }
    public class UserCreateDto
    {
        public string? UserName { get; set; }

        public string? FullName { get; set; }

        public string? PasswordHash { get; set; }

        public string? Mobile { get; set; }

        public string? Email { get; set; }

        public string? EmployeeID { get; set; }

        public int? CompanyID { get; set; }
        public string? ModuleID { get; set; }
        public int? RoleID { get; set; }

        public string? CreatedBy { get; set; }

    }
    public class UserDto
    {
        public int UserID { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? EmployeeID { get; set; }
        public int? CompanyID { get; set; }
        //public int? ModuleID { get; set; }
        //public int[]? ModuleID { get; set; }
        //public int? RoleID { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
