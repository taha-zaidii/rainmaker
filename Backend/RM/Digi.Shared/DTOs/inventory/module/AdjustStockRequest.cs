using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class AdjustStockRequest
    {
        [Required]
        public int ItemId { get; set; }
        
        [Required]
        public int WarehouseId { get; set; }
        
        [Required]
        public decimal Quantity { get; set; }
        
        [Required]
        public string TransactionType { get; set; } = string.Empty; // IN, OUT, ADJUSTMENT
        
        [Required]
        public string ReferenceType { get; set; } = string.Empty; // MANUAL, SYSTEM, CORRECTION
        
        public int? ReferenceId { get; set; }
        
        public decimal? UnitCost { get; set; }
        
        public string? Remarks { get; set; }
        
        public string? Reason { get; set; }
        
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }
}
