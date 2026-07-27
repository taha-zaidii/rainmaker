using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class PeriodDto
    {
        public int PeriodID { get; set; }
        
        [Required(ErrorMessage = "Year Name is required")]
        [StringLength(50, ErrorMessage = "Year Name cannot exceed 50 characters")]
        public string YearName { get; set; }
        
        [Required(ErrorMessage = "Start Date is required")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "End Date is required")]
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class PeriodDetailDto
    {
        public int PeriodDetID { get; set; }
        public int PeriodID { get; set; }
        public string Period { get; set; }
        
        [Required(ErrorMessage = "Period Name is required")]
        [StringLength(50, ErrorMessage = "Period Name cannot exceed 50 characters")]
        public string PeriodName { get; set; }
        
        [Required(ErrorMessage = "Start Date is required")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "End Date is required")]
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
    }

    public class CreatePeriodRequest
    {
        [Required(ErrorMessage = "Year Name is required")]
        [StringLength(50, ErrorMessage = "Year Name cannot exceed 50 characters")]
        public string YearName { get; set; }
        
        [Required(ErrorMessage = "Start Date is required")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "End Date is required")]
        public DateTime EndDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public int CompanyID { get; set; }
    }

    public class UpdatePeriodRequest : CreatePeriodRequest
    {
        [Required(ErrorMessage = "Period ID is required")]
        public int PeriodID { get; set; }
    }

    public class CreatePeriodDetailRequest
    {
        [Required(ErrorMessage = "Period ID is required")]
        public int PeriodID { get; set; }
        
        [Required(ErrorMessage = "Period is required")]
        [StringLength(20, ErrorMessage = "Period cannot exceed 20 characters")]
        public string Period { get; set; }
        
        [Required(ErrorMessage = "Period Name is required")]
        [StringLength(50, ErrorMessage = "Period Name cannot exceed 50 characters")]
        public string PeriodName { get; set; }
        
        [Required(ErrorMessage = "Start Date is required")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "End Date is required")]
        public DateTime EndDate { get; set; }
    }

    public class UpdatePeriodDetailRequest : CreatePeriodDetailRequest
    {
        [Required(ErrorMessage = "Period Detail ID is required")]
        public int PeriodDetID { get; set; }
    }
}
