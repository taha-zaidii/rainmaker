namespace Digi.Shared.DTOs.admin.module
{
    public sealed class UserBulkTemplateQueryDto
    {
        public int CompanyID { get; set; }
        public bool IncludeExistingUsers { get; set; } = false;
        public bool IncludeInactiveEmp { get; set; } = false;
    }

    public sealed class UserBulkTemplateRowDto
    {
        public int EmployeeID { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public int? RoleID { get; set; }
        public string? RoleName { get; set; }
        public bool IsActive { get; set; } = true;
        public bool MustChangePassword { get; set; } = true;
        public int? LineManagerID { get; set; }
        public string? LineManagerName { get; set; }
        public string? LineManagerEmail { get; set; }
        public bool HasExistingUser { get; set; }
        public int? ExistingUserID { get; set; }
    }

    public sealed class RoleLookupDto
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public sealed class UserBulkImportRequestDto
    {
        public int CompanyID { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool AllowUpdateExistingUsers { get; set; } = true;
        public bool AllowAutoFixManagerEmail { get; set; } = true;
        public List<UserBulkImportRowDto> Rows { get; set; } = new();
    }

    public sealed class UserBulkImportRowDto
    {
        public int? RowNo { get; set; }
        public int? EmployeeID { get; set; }
        public string? EmployeeCode { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public int? RoleID { get; set; }
        public string? RoleName { get; set; }
        public string? PasswordHash { get; set; }
        public string? SecurityStamp { get; set; }
        public bool? IsActive { get; set; }
        public bool? MustChangePassword { get; set; }
    }

    public sealed class UserBulkImportSummaryDto
    {
        public int TotalRows { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }
    }

    public sealed class UserBulkImportResultRowDto
    {
        public int? RowNo { get; set; }
        public int? EmployeeID { get; set; }
        public string? EmployeeCode { get; set; }
        public int? FinalEmployeeID { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public int? RoleID { get; set; }
        public string? RoleName { get; set; }
        public string? ActionTaken { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public sealed class UserBulkImportResponseDto
    {
        public UserBulkImportSummaryDto Summary { get; set; } = new();
        public List<UserBulkImportResultRowDto> Details { get; set; } = new();
    }

    public sealed class UserBulkImportFileUploadDto
    {
        public int CompanyID { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool AllowUpdateExistingUsers { get; set; } = true;
        public bool AllowAutoFixManagerEmail { get; set; } = true;
        public Microsoft.AspNetCore.Http.IFormFile? File { get; set; }
    }
}
