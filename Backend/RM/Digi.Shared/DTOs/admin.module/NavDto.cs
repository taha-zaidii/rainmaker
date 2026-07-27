using System.ComponentModel.DataAnnotations;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.admin.module
{
    public class NavCreateDto
    {
        [Required]
        [StringLength(100)]
        public string? DisplayName { get; set; }
        public int? ParentID { get; set; }
        public string? RouteName { get; set; }
        public string? IconURL { get; set; }
        public int? ModuleId { get; set; }
        public bool IsActive { get; set; }
        public int? DisplayOrder { get; set; }

        [Required]
        public string? CreatedBy { get; set; }
    }

    public class NavUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string? DisplayName { get; set; }
        public int? ParentID { get; set; }
        public string? RouteName { get; set; }
        public string? IconURL { get; set; }
        public int? ModuleId { get; set; }
        public bool IsActive { get; set; }
        public int? DisplayOrder { get; set; }

        [Required]
        public string? UpdatedBy { get; set; }
    }
    public class NavDto
    {
        public int? NavId { get; set; }
        public string? DisplayName { get; set; }
        public int? ParentID { get; set; }

        public NavNameDto ParentName { get ; set; } = new NavNameDto();
        public string? RouteName { get; set; }
        public string? IconURL { get; set; }
        public int? ModuleId { get; set; }
        public ModuleNameDto? Module { get; set; } = new ModuleNameDto();
        public bool IsActive { get; set; }
        public int? DisplayOrder { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? IsDeletedOn { get; set; }
    }
}
