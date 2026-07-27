using System.ComponentModel.DataAnnotations;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.gen.module
{
    public class StateCreateDto
    {
        [Required]
        [StringLength(100)]
        public string? StateName { get; set; }

        public int? CountryID { get; set; }
        public int? CompanyID { get; set; }

        [Required]
        public string? CreatedBy { get; set; }
    }

    public class StateUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string? StateName { get; set; }

        public int? CountryID { get; set; }
        public int? CompanyID { get; set; }

        [Required]
        public string? UpdatedBy { get; set; }
    }

    public class StateDto
    {
        public int StateID { get; set; }
        public string? StateName { get; set; }
        public int? CountryID { get; set; }
        public CountryNameDto? Country { get; set; } = new CountryNameDto();
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
