using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.gen.module
{
    public class ModuleCreateDto
    {
        [Required]
        [StringLength(100)]
        public string? ModuleName { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public string? CreatedBy { get; set; }
    }

    public class ModuleUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string? ModuleName { get; set; }

        [Required]
        public string? UpdatedBy { get; set; }
    }

    public class ModuleDto
    {
        public int ModuleID { get; set; }
        public string? ModuleName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? IsDeletedOn { get; set; }
    }
    

}
