using System.ComponentModel.DataAnnotations;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.gen.module
{   
    public class DesignationCreateDto
    {
        [Required]
        [StringLength(100)]
        public string? DesignationName { get; set; }
        public string? Description { get; set; }
        public int? CompanyID { get; set; }

        public int? DepartmentID { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public string? CreatedBy { get; set; }
    }

    public class DesignationUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string? DesignationName { get; set; }
        public string? Description { get; set; }

        public int? DepartmentID { get; set; }

        [Required]
        public string? UpdatedBy { get; set; }
    }

    public class DesignationCompanyDto
    {
        public string? CompanyName { get; set; }
    }

    public class DesignationDeparmentDto
    {
        public string? DepartmentName { get; set; }
    }
    public class DesignationDto
    {
        public int DesignationID { get; set; }
        public string? DesignationName { get; set; }
        public string? Description { get; set; }
        public int? CompanyID { get; set; }
        public CompanyNameDto? Company {  get; set; } = new CompanyNameDto();
        public int? DepartmentID { get; set; }
        public DepartmentNameDto? Deparment { get; set; } = new DepartmentNameDto();   
        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? IsDeletedOn { get; set; }
    }
}
