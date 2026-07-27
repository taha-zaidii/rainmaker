using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.admin.module
{
    public class RoleCreateResultDto
    {
        public int Code { get; set; }
        public int Success { get; set; }
        public string Message { get; set; }
        public int? NewRoleID { get; set; }
    }

    // Role DTOs
    public class RoleCreateDto
    {
        [MaxLength(25)]
        public string RoleName { get; set; }
        public string? RoleCode { get; set; }
        public string? RoleCategory { get; set; } = "CUSTOM";
        public int? AccessScopeID { get; set; }
        [MaxLength(250)]
        public string? Description { get; set; }
        public int? ParentRoleId { get; set; }
        public bool IsSystemRole { get; set; }
        public bool IsSystemLocked { get; set; }
        public int CompanyId { get; set; }
        public int ModuleID { get; set; }
        public int PackageID { get; set; }
        public bool CanManageUsers { get; set; }
        public bool CanImportUsers { get; set; }
        public bool CanExportUsers { get; set; }
        public string? EmployeeCode { get; set; }
        public List<RolePermissionDto> Permissions { get; set; } = new();
    }

    public class RoleUpdateDto : RoleCreateDto
    {
        public int RoleID { get; set; }
    }

    public class RolePermissionDto
    {
        public int FeatureID { get; set; }
        public int ModuleID { get; set; }
        public int? PermissionID { get; set; }
        public int NavID { get; set; }
        public bool IsAllowed { get; set; }
    }
    public class RolePermissionListDto
    {
        public int? ParentID { get; set; }
        public int? ModuleID { get; set; }
        public string? ModuleName { get; set; }
        [JsonIgnore]
        public int? PackageID { get; set; }
        [JsonIgnore]
        public string? PackageName { get; set; }
        public int? RoleID { get; set; }
        public int? NavID { get; set; }
        public string? NavName { get; set; }
        public int? PermissionID { get; set; }
        public int? FeatureID { get; set; }
        public string? PermissionName { get; set; }
        public bool? IsAssigned { get; set; }
        public bool? IsAllowed { get; set; }
    }

    //public class RolePermissionListDto
    //{
    //    public int? FeatureID { get; set; }
    //    public int? ParentID { get; set; }
    //    public int? ModuleID { get; set; }
    //    public string? ModuleName { get; set; }
    //    public int? RoleID { get; set; }
    //    public int? NavID { get; set; }
    //    public string? NavName { get; set; }
    //    public int? PermissionID { get; set; }
    //    public string? PermissionName { get; set; }
    //    public bool? IsAssigned { get; set; }
    //    public bool? IsAllowed { get; set; }
    //}
    public class ModuleHierarchicalDto
    {
        public int? ModuleID { get; set; }
        public string? ModuleName { get; set; }
        public int? PackageID { get; set; }
        public string? PackageName { get; set; }
        public List<NavHierarchicalDto> Navigations { get; set; } = new();
    }


    public class NavHierarchicalDto
    {
        public int NavID { get; set; }
        public string NavName { get; set; }

        public List<PermissionHierarchicalDto> Permissions { get; set; } = new();
        public List<NavHierarchicalDto> Children { get; set; } = new();
    }

    public class PermissionHierarchicalDto
    {
        public int FeatureID { get; set; }
        public int PermissionID { get; set; }
        public string PermissionName { get; set; }
        public bool? IsAssigned { get; set; }
        public bool? IsAllowed { get; set; }
    }

    public class AccessScopeDto
    {
        public int ScopeID { get; set; }
        public string ScopeCode { get; set; } = string.Empty;
        public string ScopeName { get; set; } = string.Empty;
        public int ScopeRank { get; set; }
        public bool IsActive { get; set; }
    }


    public class RoleDto
    {
        public int RoleID { get; set; }
        public string? RoleCode { get; set; }
        public string RoleName { get; set; }
        public string? RoleCategory { get; set; }
        public int? AccessScopeID { get; set; }
        public string Description { get; set; }
        public int? ParentRoleID { get; set; }
        //public int? ModuleID { get; set; }
        //public string? ModuleName { get; set; }
        public int? PackageID { get; set; }
        public string? PackageName { get; set; }
        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystemLocked { get; set; }
        public bool CanManageUsers { get; set; }
        public bool CanImportUsers { get; set; }
        public bool CanExportUsers { get; set; }
        // public int PermissionCount { get; set; }
        public List<RolePermissionListDto> Permissions { get; set; } = new();
    }

    public class RoleDeleteResponseDto
    {
        public int Code { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
