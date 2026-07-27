using System.ComponentModel.DataAnnotations;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.admin.module
{
    public class PermissionCreateDto
    {
        public string? PermissionName { get; set; }

        public string? PermissionCode { get; set; }

        public int? ModuleID { get; set; }

        public DateTime? CreatedOn { get; set; }

        public string? CreatedBy { get; set; }
    }

    public class PermissionUpdateDto
    {

        public string? PermissionName { get; set; }

        public string? PermissionCode { get; set; }

        public int? ModuleID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class PermissionDto
    {
        public int PermissionID { get; set; }
        public string? PermissionName { get; set; }
        public string? PermissionCode { get; set; }
        public int? ModuleID { get; set; }
        public ModuleNameDto Module { get; set; } = new ModuleNameDto();
        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? IsDeletedOn { get; set; }
    }
}
