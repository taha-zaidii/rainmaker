using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class BatchTransferRequest
    {
        [Required]
        public int BatchID { get; set; }
        
        [Required]
        public int FromWarehouseID { get; set; }
        
        [Required]
        public int ToWarehouseID { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }
        
        public string? Remarks { get; set; }
        
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }
}
