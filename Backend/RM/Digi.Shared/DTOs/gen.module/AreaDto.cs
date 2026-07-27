using System.ComponentModel.DataAnnotations;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.gen.module
{
    public class AreaCreateDto
    {
        [Required]
        [StringLength(100)]
        public string? AreaName { get; set; }
        public int? CountryID { get; set; }
        public int? StateID { get; set; }
        public int? CityID { get; set; }
        public int? CompanyID { get; set; }
        public string? Description { get; set; }

        [Required]
        public string? CreatedBy { get; set; }
    }

    public class AreaUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string? AreaName { get; set; }
        public int? CountryID { get; set; }
        public int? StateID { get; set; }
        public int? CityID { get; set; }
        public int? CompanyID { get; set; }
        public string? Description { get; set; }

        [Required]
        public string? UpdatedBy { get; set; }
    }

    public class AreaDto
    {
        public int AreaID { get; set; }
        public string? AreaName { get; set; }

        public string? Description { get; set; }
        public int? CountryID { get; set; }
        public CountryNameDto? Country { get; set; } = new CountryNameDto();
        public int? StateID { get; set; }
        public StateNameDto? State { get; set; } = new StateNameDto();
        public int? CityID { get; set; }
        public CityNameDto? City { get; set; } = new CityNameDto();

        public int? CompanyID { get; set; }
        public CompanyNameDto? Company { get; set; } = new CompanyNameDto();

        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? IsDeletedOn { get; set; }
    }
}