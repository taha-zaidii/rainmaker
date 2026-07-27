using System.ComponentModel.DataAnnotations;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.gen.module
{
    public class CityCreateDto
    {
        [Required]
        [StringLength(100)]
        public string? CityName { get; set; }

        public int? StateID { get; set; }
        public int? CompanyID { get; set; }

        [Required]
        public string? CreatedBy { get; set; }
    }

    public class CityUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string? CityName { get; set; }

        public int? StateID { get; set; }
        public int? CompanyID { get; set; }

        [Required]
        public string? UpdatedBy { get; set; }
    }


    public class CityDto
    {
        public int CityID { get; set; }
        public string? CityName { get; set; }
        public int? StateID { get; set; }
        public StateNameDto? States { get; set; } = new StateNameDto();
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
