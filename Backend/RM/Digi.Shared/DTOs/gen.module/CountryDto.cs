using Digi.Shared.DTOs.admin.module;
using System.ComponentModel.DataAnnotations;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.gen.module
{
    public class CountryCreateDto
    {
        [Required]
        [StringLength(100)]
        public string? CountryName { get; set; }
        public int? CompanyID { get; set; }
        public string? CountryCode { get; set; }

        public string? PhoneCode { get; set; }

        [Required]
        public string? CreatedBy { get; set; }
    }

    public class CountryUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string? CountryName { get; set; }
        //public int? CompanyID { get; set; }
        public string? CountryCode { get; set; }
        
        public string? PhoneCode { get; set; }

        [Required]
        public string? UpdatedBy { get; set; }
    }

    public class CountryDto
    {
        public int CountryID { get; set; }
        public int? CompanyID { get; set; }
        public CompanyNameDto Company { get; set; } = new CompanyNameDto();
        public string? CountryName { get; set; }
        
        public string? CountryCode { get; set; }
        
        public string? PhoneCode { get; set; }
        
        public bool IsActive { get; set; }
        
        public DateTime? CreatedOn { get; set; }
        
        public string? CreatedBy { get; set; }
        
        public DateTime? UpdatedOn { get; set; }
        
        public string? UpdatedBy { get; set; }
        
        public bool? IsDeleted { get; set; }
        
        public DateTime? IsDeletedOn { get; set; }
    }
}
