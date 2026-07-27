using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class TransferStockRequest
    {
        [Required]
        public int ItemId { get; set; }
        
        [Required]
        public int FromWarehouseId { get; set; }
        
        [Required]
        public int ToWarehouseId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }
        
        public decimal? UnitCost { get; set; }
        
        public string? Remarks { get; set; }
        
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }
}
