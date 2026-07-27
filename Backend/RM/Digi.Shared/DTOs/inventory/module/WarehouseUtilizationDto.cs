namespace Digi.Shared.DTOs.inventory.module
{
    public class WarehouseUtilizationDto
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public decimal TotalCapacity { get; set; }
        public decimal UsedCapacity { get; set; }
        public decimal AvailableCapacity { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalValue { get; set; }
    }
}
