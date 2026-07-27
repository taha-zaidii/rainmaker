using System.ComponentModel.DataAnnotations;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.gen.module
{
    public class CurrencyCreateDto
    {
        [Required]
        [StringLength(100)]
        public string? CurrencyName { get; set; }

        public string? CurrencyCode { get; set; }

        public int? CountryID { get; set; }
        public string? Symbol { get; set; }


        public bool? IsBaseCurrency { get; set; }

        public int? CompanyID { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public string? CreatedBy { get; set; }
    }

    public class CurrencyUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string? CurrencyName { get; set; }

        public string? CurrencyCode { get; set; }

        public int? CountryID { get; set; }
        public string? Symbol { get; set; }


        public bool? IsBaseCurrency { get; set; }

        public int? CompanyID { get; set; }

        [Required]
        public string? UpdatedBy { get; set; }
    }

    public class CurrencyDto
    {
        public int CurrencyID { get; set; }

        public string? CurrencyCode { get; set; }

        public string? CurrencyName { get; set; }
        public int? CountryID { get; set; }

        public CountryNameDto Country { get; set; } = new CountryNameDto();

        public string? Symbol { get; set; }


        public bool? IsBaseCurrency { get; set; }

        public int? CompanyID { get; set; }

        public CompanyNameDto Company { get; set; } = new CompanyNameDto();

        public bool IsActive { get; set; }

        public DateTime? CreatedOn { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public string? UpdatedBy { get; set; }

        public bool? IsDeleted { get; set; }

        public DateTime? IsDeletedOn { get; set; }
    }
}
