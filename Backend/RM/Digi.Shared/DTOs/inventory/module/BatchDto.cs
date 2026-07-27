using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class ItemBatchDto
    {
        public int BatchID { get; set; }
        public int ProductID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal QuantityAvailableInBatch { get; set; }
        public decimal UnitPrice { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int CompanyID { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public int DaysToExpiry { get; set; }
        public string ExpiryStatus { get; set; } = string.Empty; // 'Good', 'Expiring Soon', 'Expired'
    }

    public class BatchManagementDto
    {
        public int BatchID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal QuantityAvailable { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int DaysToExpiry { get; set; }
        public string ExpiryStatus { get; set; } = string.Empty;
        public bool IsExpired { get; set; }
        public bool IsExpiringSoon { get; set; }
    }

    public class CreateBatchRequest
    {
        [Required]
        public int ProductID { get; set; }
        [Required]
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        [Required]
        public decimal QuantityAvailableInBatch { get; set; }
        [Required]
        public decimal UnitPrice { get; set; }
        [Required]
        public int WarehouseID { get; set; }
    }

    public class UpdateBatchRequest : CreateBatchRequest
    {
        [Required]
        public int BatchID { get; set; }
    }

}
