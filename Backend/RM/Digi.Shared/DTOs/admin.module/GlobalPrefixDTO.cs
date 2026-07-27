using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.admin.module
{
    public class GlobalPrefixDTO
    {
        public int? PrefixID { get; set; }

        public int? NavID { get; set; }

        [StringLength(10)]
        public string? Separator { get; set; }

        public int? NumberOfDigit { get; set; }

        public bool? LeadingZero { get; set; }

        [StringLength(10)]
        public string? Prefix { get; set; }

        public bool? IncludeBU { get; set; }

        public bool? IncludeMonthDigit { get; set; }

        public bool? IncludeMonthName { get; set; }

        public bool? RefreshEachMonth { get; set; }

        public int? IncludeYearPrfix { get; set; }

        [StringLength(10)]
        public string? AvailVal { get; set; }

        [Required]
        public int CompanyID { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class GlobalPrefixFilterDTO
    {
        [Required]
        public int CompanyID { get; set; }

        public string? Search { get; set; }

        public int? NavID { get; set; }

        public bool? IsActive { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string? SortBy { get; set; } = "PrefixID";

        public string? SortOrder { get; set; } = "DESC";
    }
}
