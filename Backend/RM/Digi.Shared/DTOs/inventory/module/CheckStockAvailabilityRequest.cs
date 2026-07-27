using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class CheckStockAvailabilityRequest
    {
        [Required]
        public int ItemId { get; set; }
        
        [Required]
        public int WarehouseId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Required quantity must be greater than 0")]
        public decimal RequiredQuantity { get; set; }
        
        public string? Remarks { get; set; }
    }
}
