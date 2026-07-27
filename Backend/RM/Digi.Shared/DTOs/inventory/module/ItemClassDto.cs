using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class ItemClassDto
    {
        public int ItemClassID { get; set; }
        
        [Required(ErrorMessage = "Item Class Name is required")]
        [StringLength(100, ErrorMessage = "Item Class Name cannot exceed 100 characters")]
        public string ItemClassName { get; set; }
        
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }
        public bool IsGoods { get; set; } = false;
        public bool IsCombo { get; set; } = false;
        public bool IsService { get; set; } = false;
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class CreateItemClassRequest
    {
        [Required(ErrorMessage = "Item Class Name is required")]
        [StringLength(100, ErrorMessage = "Item Class Name cannot exceed 100 characters")]
        public string ItemClassName { get; set; }
        
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }
        public bool IsGoods { get; set; } = false;
        public bool IsCombo { get; set; } = false;
        public bool IsService { get; set; } = false;
        public int CompanyID { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class UpdateItemClassRequest : CreateItemClassRequest
    {
        [Required(ErrorMessage = "Item Class ID is required")]
        public int ItemClassID { get; set; }
    }

    public class DeleteItemClassDto
    {
        public int ItemClassID { get; set; }
        public int CompanyID { get; set; }
        public string? UpdatedBy { get; set; } = string.Empty;
    }

    public class BulkDeleteItemClassRequest
    {
        public List<DeleteItemClassDto> Items { get; set; } = new();
    }

    public class BulkDeleteItemClassResult
    {
        public int Requested { get; set; }
        public int Deleted { get; set; }
        public int Skipped { get; set; }
        public List<int> DeletedItemClassIDs { get; set; } = new();
        public List<int> SkippedItemClassIDs { get; set; } = new();
    }

    public class ToggleItemClassStatusDto
    {
        public int ItemClassID { get; set; }
        public int CompanyID { get; set; }
        public string UpdatedBy { get; set; }
    }
}