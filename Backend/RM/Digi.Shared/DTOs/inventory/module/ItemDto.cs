using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class ItemDto
    {
        public int ItemID { get; set; }        
        [Required(ErrorMessage = "Item Name is required")]
        [StringLength(255, ErrorMessage = "Item Name cannot exceed 255 characters")]
        public string ItemName { get; set; } = "";
        public string ItemCode { get; set; } = "";
        public string Description { get; set; } = "";
        public string Barcode { get; set; } = "";
        [Required(ErrorMessage = "Category is required")]
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = "";
        [Required(ErrorMessage = "Brand is required")]
        public int BrandID { get; set; }
        public string BrandName { get; set; } = "";
        [Required(ErrorMessage = "Vendor is required")]
        public int VendorID { get; set; }
        public string VendorName { get; set; } = "";
        [Required(ErrorMessage = "UOM is required")]
        public int SalesUOMID { get; set; }
        public string SalesUOMName { get; set; } = "";
        public int? PurchaseUOMID { get; set; }
        public string PurchaseUOMName { get; set; } = "";
        public bool IsFixedAssets { get; set; }
        public bool HasBatch { get; set; }
        public bool HasSerial { get; set; }        
        [Required(ErrorMessage = "Item Class is required")]
        public int ItemClassID { get; set; }
        public string ItemClassName { get; set; } = "";
        public decimal MinStockLevel { get; set; }
        public decimal MaxStockLevel { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal MinOrderQuantity { get; set; }
        public decimal MaxOrderQuantity { get; set; }
        public int LeadTime { get; set; }
        public int? ShelfLife { get; set; }
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; } = "";
        public string? ItemImageUrl { get; set; }
        public int? AttachmentDetailID { get; set; }
        public IFormFile? AttachmentFile { get; set; }
        public string? AttachmentURL { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public int CompanyID { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        
        // Additional fields from documentation
        public string SKU { get; set; } = "";
        public string? HSNCode { get; set; } = "";
        public decimal? SalesTaxes { get; set; }
        public decimal? PurchaseTaxes { get; set; }
        public decimal? DiscountRate { get; set; }
        public decimal? purchaseQty { get; set; }
        public decimal? purchaseUnitPrice { get; set; }
        public decimal? purchaseUOMType { get; set; }
        public string? priceListName { get; set; }
        public decimal? minQty { get; set; }
        public decimal? individualPrice { get; set; }
        public int? WarrantyPeriod { get; set; }
        public string? WarrantyType { get; set; } // 'None', 'Manufacturer', 'Extended'
        public string? Notes { get; set; }
        public string ItemStatus { get; set; } = "Pending"; // 'Pending', 'Active', 'Inactive', 'Discontinued'
        public bool TrackInventory { get; set; }
        
        // Fixed Assets fields
        public string? AssetCode { get; set; }
        public decimal? DepreciationRate { get; set; }
        public DateTime? AssetPurchaseDate { get; set; }
        public DateTime? AssetWarrantyExpiryDate { get; set; }
        
        // UOM Conversion fields
        public decimal? PiecesPerBox { get; set; }
        public decimal? BoxesPerCarton { get; set; }
        public decimal QuantityPerUnit { get; set; } = 1;
        public bool SerialNumberRequired { get; set; }

        // Stock Level fields
        public decimal? StockQuantity { get; set; }
        public decimal? ReservedQuantity { get; set; }
        //public string PCTCode { get; set; }        
        //[Required(ErrorMessage = "Item Code is required")]
        //[StringLength(50, ErrorMessage = "Item Code cannot exceed 50 characters")]
        //[Required(ErrorMessage = "Purchase UOM is required")]
        //public int PurchaseUOMID { get; set; }
        //public string PurchaseUOMName { get; set; }
        //[Required(ErrorMessage = "Sales UOM is required")]
        //public int SalesUOMID { get; set; }
        //public string SalesUOMName { get; set; }
    }

    public class CreateItemRequest
    {
        [Required(ErrorMessage = "Item Class is required")]
        public int ItemClassID { get; set; } // 22, 23, 24, or 25
        
        public string? ItemName { get; set; } // Optional - Auto-generated if empty
        public int CompanyID { get; set; }
        [Required(ErrorMessage = "Created By is required")]
        public string CreatedBy { get; set; } = "";
        public int? PurchaseUOMID { get; set; }
        public int? SalesUOMID { get; set; }
        
        // Optional fields
        public string? Description { get; set; }
        public string? Barcode { get; set; } // Auto-generated if empty
        public int? CategoryID { get; set; } // Required for Inventory (22) and Capital Assets (25)
       
     
        public string? SKU { get; set; } // Auto-generated if empty
        public string? HSNCode { get; set; } // Required for Inventory (22)
        public decimal? PurchasePrice { get; set; }
        public decimal? SalePrice { get; set; }
        
        // Purchase Tab
        public int? VendorID { get; set; }
        public int? BrandID { get; set; }
        public decimal? purchaseQty { get; set; }
        public decimal? purchaseUnitPrice { get; set; }
        public decimal? purchaseUOMType { get; set; }

        //Pricing Tab
        public decimal? DiscountRate { get; set; }
        public string? priceListName { get; set; }
        public decimal? minQty { get; set; }
        public decimal? individualPrice { get; set; }

        public decimal? ReorderLevel { get; set; } // Only for Inventory items
        public decimal? MinOrderQuantity { get; set; } // Only for Inventory items
        public decimal? MaxOrderQuantity { get; set; } // Only for Inventory items
        public int? LeadTime { get; set; } // Only for Inventory items
        public decimal? Weight { get; set; } // Required for Inventory (22)
        public string? Dimensions { get; set; } // Required for Inventory (22)
        public int? ShelfLife { get; set; } // Required for Inventory (22)
        public decimal? SalesTaxes { get; set; }
        public decimal? PurchaseTaxes { get; set; }
       
        public int? WarrantyPeriod { get; set; }
        public string? WarrantyType { get; set; } // 'None', 'Manufacturer', 'Extended'
        public string? Notes { get; set; }
        public string? ItemStatus { get; set; } = "Pending"; // Default: 'Pending'
        public bool? TrackInventory { get; set; } // Auto-set based on ItemClass
        
        // Fixed Assets fields
        public bool? IsFixedAssets { get; set; } // Auto-set to true when ItemClassID = 25
        public string? AssetCode { get; set; }
        public decimal? DepreciationRate { get; set; }
        public string? AssetPurchaseDate { get; set; } // ISO format: YYYY-MM-DD
        public string? AssetWarrantyExpiryDate { get; set; } // ISO format: YYYY-MM-DD
        
        // UOM Conversion fields
        public decimal? PiecesPerBox { get; set; } // For Box/Carton UOM
        public decimal? BoxesPerCarton { get; set; } // For Box/Carton UOM
        public decimal? QuantityPerUnit { get; set; } = 1; // For PCS/Unit UOM
        public bool? SerialNumberRequired { get; set; } // For PCS/Unit UOM
        public decimal? WeightPerUnit { get; set; } // For Weight UOM (converted to Base UOM)
        
        // Legacy fields (keeping for backward compatibility)
        public bool HasBatch { get; set; }
        public bool HasSerial { get; set; }
        public decimal MinStockLevel { get; set; }
        public decimal MaxStockLevel { get; set; }
        public string ItemCode { get; set; } = ""; // Auto-generated if not provided
        public string? ItemImageUrl { get; set; }
        public IFormFile? AttachmentFile { get; set; }
        public string? AttachmentURL { get; set; }
        public string? CompanyName { get; set; }
        public bool RemoveImage { get; set; } = false;
        public int? EmployeeID { get; set; }
    }

    public class UpdateItemRequest : CreateItemRequest
    {
        [Required(ErrorMessage = "Item ID is required")]
        public int ItemID { get; set; }
        public int? AttachmentDetailID { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class deleteItemDto
    {
        public int? CompanyID { get; set; }
        public string? UpdatedBy { get; set; } = string.Empty;
        public int ItemID { get; set; }        
    }

    public class BulkDeleteItemsRequest
    {
        public int CompanyID { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public List<deleteItemDto> Items { get; set; } = new();
    }

    public class BulkDeleteItemsResult
    {
        public int Requested { get; set; }
        public int Deleted { get; set; }
        public int Skipped { get; set; } // reserved
        public List<int> DeletedItemIDs { get; set; } = new();
        public List<int> SkippedItemIDs { get; set; } = new();
        public int DeletedCount { get; set; } // Alias for Deleted
    }

    // Filter DTO for advanced filtering
    public class FilterItemRequest
    {
        [Required]
        public int CompanyID { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; } // Search in ItemCode, ItemName, Description, Barcode, SKU
        public int? CategoryID { get; set; }
        public int? BrandID { get; set; }
        public int? VendorID { get; set; }
        public int? ItemClassID { get; set; } // 22, 23, 24, or 25
        public string? Status { get; set; } // 'Active', 'Inactive', 'Pending', 'Discontinued'
        public bool? IsFixedAssets { get; set; }
        public bool? TrackInventory { get; set; }
        public string? SortBy { get; set; } // ItemName, ItemCode, PurchasePrice, SalePrice, CreatedOn
        public string? SortOrder { get; set; } = "ASC"; // 'ASC' or 'DESC'
    }

    // Item Class Constants
    public static class ItemClassConstants
    {
        public const int INVENTORY = 22;
        public const int NON_INVENTORY = 23;
        public const int SERVICES = 24;
        public const int CAPITAL_ASSETS = 25;
    }

    // Item Status Constants
    public static class ItemStatusConstants
    {
        public const string PENDING = "Pending";
        public const string ACTIVE = "Active";
        public const string INACTIVE = "Inactive";
        public const string DISCONTINUED = "Discontinued";
    }

    public class ItemBasicDto
    {
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = "";
        public string Barcode { get; set; } = "";
        public int? CategoryID { get; set; }
        public string CategoryName { get; set; } = "";
        public int? BrandID { get; set; }
        public string BrandName { get; set; } = "";
        public int? VendorID { get; set; }
        public string VendorName { get; set; } = "";
        public int? SalesUOMID { get; set; }
        public string SalesUOMName { get; set; } = "";
        public int? PurchaseUOMID { get; set; }
        public string PurchaseUOMName { get; set; } = "";
        public int? ItemClassID { get; set; }
        public string ItemClassName { get; set; } = "";
        public decimal MinStockLevel { get; set; }
        public decimal MaxStockLevel { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal MinOrderQuantity { get; set; }
        public decimal MaxOrderQuantity { get; set; }
        public int? LeadTime { get; set; }
        public int? ShelfLife { get; set; }
        public decimal? Weight { get; set; }
        public string Dimensions { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public int CompanyID { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public string SKU { get; set; } = "";
        public string? HSNCode { get; set; } = "";
        public decimal? SalesTaxes { get; set; }
        public decimal? PurchaseTaxes { get; set; }
        public decimal? DiscountRate { get; set; }
        public int? WarrantyPeriod { get; set; }
        public string? WarrantyType { get; set; } 
        public string? Notes { get; set; }
    }

    public class ToggleItemStatusDto
    {
        public int ItemID { get; set; }
        public int CompanyID { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class ToggleItemContinueStatusDto
    {
        public int ItemID { get; set; }
        public int CompanyID { get; set; }
        public string? UpdatedBy { get; set; }
    }
}