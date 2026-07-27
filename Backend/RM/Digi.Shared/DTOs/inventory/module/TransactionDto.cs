using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class ItemTransactionDto
    {
        public int TransactionID { get; set; }
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int PeriodID { get; set; }
        public string PeriodName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty; // Receipt, Issue, Transfer, Adjustment, Return
        public DateTime TransactionDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string ReferenceType { get; set; } = string.Empty; // PO, SO, GRN, SRN, Transfer, Adjustment
        public int ReferenceID { get; set; }
        public int? BatchID { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int CompanyID { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class MovementReportDto
    {
        public int TransactionID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public int ReferenceID { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class StockAdjustmentRequest
    {
        [Required]
        public int ItemID { get; set; }
        [Required]
        public int WarehouseID { get; set; }
        [Required]
        public decimal Quantity { get; set; }
        [Required]
        public string AdjustmentType { get; set; } = string.Empty; // Increase, Decrease
        [Required]
        public string Reason { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    public class StockTransferRequest
    {
        [Required]
        public int ItemID { get; set; }
        [Required]
        public int FromWarehouseID { get; set; }
        [Required]
        public int ToWarehouseID { get; set; }
        [Required]
        public decimal Quantity { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    public class TransactionReportRequest
    {
        public int? ItemID { get; set; }
        public int? WarehouseID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? TransactionType { get; set; }
    }
}
