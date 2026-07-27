using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class InventoryValuationReportDto
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public decimal QuantityOnHand { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
    }

    public class ABCAnalysisDto
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public decimal QuantityOnHand { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public decimal PercentageValue { get; set; }
        public decimal CumulativePercentage { get; set; }
        public string ABC_Class { get; set; } = string.Empty; // A, B, C
    }


    public class ItemCostDto
    {
        public int ItemCostID { get; set; }
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal UnitCost { get; set; }
        public string ValuationMethod { get; set; } = string.Empty; // FIFO, LIFO, Weighted Average, Standard
        public DateTime LastUpdated { get; set; }
        public int CompanyID { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateItemCostRequest
    {
        [Required]
        public int ItemID { get; set; }
        [Required]
        public int WarehouseID { get; set; }
        [Required]
        public decimal UnitCost { get; set; }
        [Required]
        public string ValuationMethod { get; set; } = string.Empty;
        public int CompanyID { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class UpdateItemCostRequest : CreateItemCostRequest
    {
        [Required]
        public int ItemCostID { get; set; }
    }

    public class InventoryValuationRequest
    {
        public int? WarehouseID { get; set; }
        public int? PeriodID { get; set; }
        public string ValuationMethod { get; set; } = "FIFO";
    }

    public class ABCAnalysisRequest
    {
        public int? WarehouseID { get; set; }
        public int? PeriodID { get; set; }
    }

    public class VendorPerformanceRequest
    {
        public int? VendorID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
