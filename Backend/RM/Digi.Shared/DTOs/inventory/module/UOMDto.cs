using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class UOMDto
    {
        public int UOMID { get; set; }
        public string? UOMKey { get; set; } = string.Empty;
        public string UOMName { get; set; } = string.Empty;
        public string UOMCode { get; set; } = string.Empty;
        public int? UOMType { get; set; }
        public string? Description { get; set; } = string.Empty;
        public decimal ConversionFactor { get; set; }
        public int? BaseUOMID { get; set; }
        public string? BaseUOMName { get; set; } = string.Empty;
        public int CompanyID { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateUOMRequest
    {
        [Required(ErrorMessage = "UOM Name is required")]
        public string UOMName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "UOM Code is required")]
        public string UOMCode { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        public decimal ConversionFactor { get; set; }
        public int UOMType { get; set; }
        public int BaseUOMID { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public int CompanyID { get; set; }
    }

    public class UpdateUOMRequest : CreateUOMRequest
    {
        [Required(ErrorMessage = "UOM ID is required")]
        public int UOMID { get; set; }
    }

    public class UOMTypeDto
    {
        public int UOMTypeID { get; set; }
        public string UOMTypeName { get; set; } = string.Empty;
    }

    public class BulkDeleteUOMRequest
    {
        public List<int> UOMIDs { get; set; } = new();
        public string UpdatedBy { get; set; } = string.Empty;
        public int CompanyID { get; set; }
    }

    public class BulkDeleteResultDto
    {
        public int UOMID { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class BulkDeleteSummaryDto
    {
        public int Total { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public List<BulkDeleteResultDto> Results { get; set; } = new();
    }
}