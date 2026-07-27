using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class StockDto
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityReserved { get; set; }
        public decimal QuantityAvailable { get; set; }
        public decimal MinLevel { get; set; }
        public decimal MaxLevel { get; set; }
        public decimal ReOrderLevel { get; set; }
        public DateTime LastUpdated { get; set; }
        public string StockStatus { get; set; }
    }

    public class StockUpdateRequest
    {
        [Required(ErrorMessage = "Item ID is required")]
        public int ItemID { get; set; }
        
        [Required(ErrorMessage = "Warehouse ID is required")]
        public int WarehouseID { get; set; }
        
        [Required(ErrorMessage = "Quantity is required")]
        public decimal Quantity { get; set; }
        
        [Required(ErrorMessage = "Transaction Type is required")]
        public string TransactionType { get; set; }
        
        [Required(ErrorMessage = "Reference Type is required")]
        public string ReferenceType { get; set; }
        
        [Required(ErrorMessage = "Reference ID is required")]
        public int ReferenceID { get; set; }
        
        public decimal UnitCost { get; set; }
        public string BatchNumber { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class InventoryValuationDto
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string WarehouseName { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string CategoryName { get; set; }
        public string BrandName { get; set; }
    }


    public class StockListRequest
    {
        public int CompanyID { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public int? CategoryID { get; set; }
        public int? WarehouseID { get; set; }
        public string? Status { get; set; }
    }

    public class StockTransactionListRequest
    {
        public int CompanyID { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public int? WarehouseID { get; set; }
        public int? ItemID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class StockUpdateRequestLatest
    {
        public int ItemID { get; set; }
        public int WarehouseID { get; set; }
        public decimal Quantity { get; set; }
        public string TransactionType { get; set; } = string.Empty; // StockIn/StockOut/Adjustment/Transfer
        public string? ReferenceType { get; set; }
        public int? ReferenceID { get; set; }
        public decimal UnitCost { get; set; } = 0;
        public string? BatchNumber { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
        public int? ToWarehouseID { get; set; }  // Transfer only

        // Set by Controller from JWT
        public int CompanyID { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    //public class CheckStockAvailabilityRequest
    //{
    //    public int ItemId { get; set; }
    //    public int WarehouseId { get; set; }
    //    public decimal RequiredQuantity { get; set; }
    //}




    // ?? Request DTOs ????????????????????????????????????????

    public class StockListRequestLatest
    {
        public int? CompanyID { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public int? CategoryID { get; set; }
        public int? WarehouseID { get; set; }
        public string? Status { get; set; }
    }

    public class StockTransactionListRequestLatest
    {
        public int CompanyID { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public int? WarehouseID { get; set; }
        public int? ItemID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    

    public class CheckStockAvailabilityRequestLatest
    {
        public int ItemId { get; set; }
        public int WarehouseId { get; set; }
        public decimal RequiredQuantity { get; set; }
    }

    // ?? Response DTOs ???????????????????????????????????????

    public class StockDtoLatest
    {
        public int StockID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int? CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public int? BrandID { get; set; }
        public string? BrandName { get; set; }
        public int? WarehouseID { get; set; }
        public string? WarehouseName { get; set; }
        public int? LocationID { get; set; }
        public string? LocationName { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal QuantityReserved { get; set; }
        public decimal QuantityAvailable { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal MaxStock { get; set; }
        public string? Unit { get; set; }
        public DateTime? LastUpdated { get; set; }
        public decimal UnitCost { get; set; }
        public decimal StockValue { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
    }

    public class StockTransactionDto
    {
        public int LedgerID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int? WarehouseID { get; set; }
        public string? WarehouseName { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string? TransactionNo { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceID { get; set; }
        public decimal Quantity { get; set; }
        public decimal PreviousQty { get; set; }
        public decimal NewQty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string? BatchNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? PerformedBy { get; set; }
        public int TotalRecords { get; set; }
    }

    public class StockUpdateResultDto
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public string? TransactionNo { get; set; }
        public decimal? NewQuantity { get; set; }
    }

    public class StockAvailabilityDto
    {
        public decimal AvailableQuantity { get; set; }
        public decimal RequiredQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public string? Message { get; set; }
    }

    public class InventoryValuationDtoLatest
    {
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public int? WarehouseID { get; set; }
        public string? WarehouseName { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class PagedResults<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    }



    // ----------------------------------------------------------
    // REQUEST DTOs
    // ----------------------------------------------------------

    /// <summary>Paginated list request</summary>
    public class StockAdjustmentListRequest
    {
        public int CompanyID { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? Status { get; set; }   // Draft | Approved | Rejected
        public int? WarehouseID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>Item line inside Create / Update requests</summary>
    public class StockAdjustmentItemRequest
    {
        [Required] public int ItemID { get; set; }
        public int? BatchID { get; set; }
        [Required] public decimal CurrentQuantity { get; set; }
        [Required] public decimal AdjustedQuantity { get; set; }
        public decimal? UnitCost { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>Create new Stock Adjustment (header + items)</summary>
    public class CreateStockAdjustmentRequest
    {
        public int CompanyID { get; set; }   // set from JWT
        public string CreatedBy { get; set; } = string.Empty; // set from JWT

        [Required] public int WarehouseID { get; set; }
        [Required] public DateTime AdjustmentDate { get; set; }
        [Required] public string AdjustmentType { get; set; } = "Manual";
        [Required] public string Reason { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public int? PeriodID { get; set; }

        [Required, MinLength(1)]
        public List<StockAdjustmentItemRequest> Items { get; set; } = new();
    }

    /// <summary>Update existing Draft adjustment</summary>
    public class UpdateStockAdjustmentRequest
    {
        [Required] public int AdjustmentID { get; set; }
        public int CompanyID { get; set; }   // set from JWT
        public string UpdatedBy { get; set; } = string.Empty; // set from JWT

        [Required] public int WarehouseID { get; set; }
        [Required] public DateTime AdjustmentDate { get; set; }
        [Required] public string AdjustmentType { get; set; } = "Manual";
        [Required] public string Reason { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public int? PeriodID { get; set; }

        [Required, MinLength(1)]
        public List<StockAdjustmentItemRequest> Items { get; set; } = new();
    }

    /// <summary>Approve or Reject a Draft adjustment</summary>
    public class ApproveRejectAdjustmentRequest
    {
        [Required] public int AdjustmentID { get; set; }
        public int CompanyID { get; set; }   // set from JWT
        public string ActionBy { get; set; } = string.Empty; // set from JWT

        /// <summary>Approve | Reject</summary>
        [Required] public string Action { get; set; } = string.Empty;
    }

    /// <summary>Delete (soft) a Draft adjustment</summary>
    public class DeleteStockAdjustmentRequest
    {
        [Required] public int AdjustmentID { get; set; }
        public int CompanyID { get; set; }   // set from JWT
        public string DeletedBy { get; set; } = string.Empty; // set from JWT
    }

    // ----------------------------------------------------------
    // RESPONSE DTOs
    // ----------------------------------------------------------

    /// <summary>Header row returned in the list</summary>
    public class StockAdjustmentDto
    {
        public int AdjustmentID { get; set; }
        public string AdjustmentNumber { get; set; } = string.Empty;
        public DateTime AdjustmentDate { get; set; }
        public string AdjustmentType { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public int TotalItems { get; set; }
        public int? PeriodID { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectedBy { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int TotalRecords { get; set; }  // for pagination
    }

    /// <summary>Detail item row</summary>
    public class StockAdjustmentItemDto
    {
        public int AdjustmentItemID { get; set; }
        public int AdjustmentID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int? BatchID { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal AdjustedQuantity { get; set; }
        public decimal Difference { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>Full adjustment detail (header + items)</summary>
    public class StockAdjustmentDetailDto
    {
        public StockAdjustmentDto Header { get; set; } = new();
        public List<StockAdjustmentItemDto> Items { get; set; } = new();
    }

    /// <summary>Result from Create / Approve SP</summary>
    public class StockAdjustmentResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? AdjustmentID { get; set; }
        public string? AdjustmentNumber { get; set; }
    }

    // ?? REQUEST DTOs ????????????????????????????????????????

    public class StockTransferListRequest
    {
        public int CompanyID { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int? FromWarehouseID { get; set; }
        public int? ToWarehouseID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class StockTransferItemRequest
    {
        [Required] public int ItemID { get; set; }
        public int? BatchID { get; set; }
        [Required] public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }

    public class CreateStockTransferRequest
    {
        public int CompanyID { get; set; }   // from JWT
        public string CreatedBy { get; set; } = string.Empty; // from JWT

        [Required] public int FromWarehouseID { get; set; }
        [Required] public int ToWarehouseID { get; set; }
        [Required] public DateTime TransferDate { get; set; }
        public string? Reason { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public int? PeriodID { get; set; }

        [Required, MinLength(1)]
        public List<StockTransferItemRequest> Items { get; set; } = new();
    }

    public class UpdateStockTransferRequest
    {
        [Required] public int TransferID { get; set; }
        public int CompanyID { get; set; }   // from JWT
        public string UpdatedBy { get; set; } = string.Empty; // from JWT

        [Required] public int FromWarehouseID { get; set; }
        [Required] public int ToWarehouseID { get; set; }
        [Required] public DateTime TransferDate { get; set; }
        public string? Reason { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public int? PeriodID { get; set; }

        [Required, MinLength(1)]
        public List<StockTransferItemRequest> Items { get; set; } = new();
    }

    /// <summary>Action = Start | Complete | Cancel</summary>
    public class ChangeTransferStatusRequest
    {
        [Required] public int TransferID { get; set; }
        public int CompanyID { get; set; }   // from JWT
        public string ActionBy { get; set; } = string.Empty; // from JWT

        [Required] public string Action { get; set; } = string.Empty;
    }

    public class DeleteStockTransferRequest
    {
        [Required] public int TransferID { get; set; }
        public int CompanyID { get; set; }   // from JWT
        public string DeletedBy { get; set; } = string.Empty; // from JWT
    }

    // ?? RESPONSE DTOs ???????????????????????????????????????

    public class StockTransferDto
    {
        public int TransferID { get; set; }
        public string TransferNumber { get; set; } = string.Empty;
        public DateTime TransferDate { get; set; }
        public int FromWarehouseID { get; set; }
        public string FromWarehouseName { get; set; } = string.Empty;
        public int ToWarehouseID { get; set; }
        public string ToWarehouseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public int TotalItems { get; set; }
        public int? PeriodID { get; set; }
        public string? ReceivedBy { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int TotalRecords { get; set; }
    }

    public class StockTransferItemDto
    {
        public int TransferItemID { get; set; }
        public int TransferID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int? BatchID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal PendingQuantity { get; set; }
    }

    public class StockTransferDetailDto
    {
        public StockTransferDto Header { get; set; } = new();
        public List<StockTransferItemDto> Items { get; set; } = new();
    }

    public class StockTransferResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? TransferID { get; set; }
        public string? TransferNumber { get; set; }
    }


    public class StockLevelDto
    {
        public int StockLevelID { get; set; }
        public int CompanyID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int? CategoryID { get; set; }
        public int? WarehouseID { get; set; }
        public string? WarehouseName { get; set; } = string.Empty;
        public decimal? CurrentStock { get; set; }
        public decimal? ReorderLevel { get; set; }
        public decimal? ReorderQuantity { get; set; }
        public decimal? MaximumLevel { get; set; }
        public decimal? MinimumLevel { get; set; }
        public decimal? AverageConsumption { get; set; }
        public int? LeadTime { get; set; }
        public decimal? SafetyStock { get; set; }
        public string? Status { get; set; } = string.Empty;
        public DateTime? NextReorderDate { get; set; }
        public string? CreatedBy { get; set; } = string.Empty;
        public DateTime? CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }

        // pagination support
        public int TotalRecords { get; set; }
    }

    // ?? RESULT DTO ???????????????????????????????????????????
    public class StockLevelResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? StockLevelID { get; set; }
    }

    // ?? LIST REQUEST ?????????????????????????????????????????
    public class StockLevelListRequest
    {
        public int CompanyID { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int? WarehouseID { get; set; }
        public string? Category { get; set; }
        public int? CategoryID { get; set; }
    }

    // ?? CREATE REQUEST ???????????????????????????????????????
    public class CreateStockLevelRequest
    {
        public int? CompanyID { get; set; }
        public int ItemID { get; set; }
        public int WarehouseID { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal ReorderQuantity { get; set; }
        public decimal MaximumLevel { get; set; }
        public decimal MinimumLevel { get; set; }
        public decimal AverageConsumption { get; set; }
        public int LeadTime { get; set; }
        public decimal SafetyStock { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    // ?? UPDATE REQUEST ???????????????????????????????????????
    public class UpdateStockLevelRequest
    {
        public int StockLevelID { get; set; }
        public int CompanyID { get; set; }
        public int ItemID { get; set; }
        public int WarehouseID { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal ReorderQuantity { get; set; }
        public decimal MaximumLevel { get; set; }
        public decimal MinimumLevel { get; set; }
        public decimal AverageConsumption { get; set; }
        public int LeadTime { get; set; }
        public decimal SafetyStock { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    // ?? DELETE REQUEST ???????????????????????????????????????
    public class DeleteStockLevelRequest
    {
        public int StockLevelID { get; set; }
        public int CompanyID { get; set; }
        public string DeletedBy { get; set; } = string.Empty;
    }

    // ?? BULK DELETE REQUEST ??????????????????????????????????
    public class BulkDeleteStockLevelRequest
    {
        public List<int> StockLevelIDs { get; set; } = new();
        public int CompanyID { get; set; }
        public string DeletedBy { get; set; } = string.Empty;
    }


    // ?? REPORT TYPES CONSTANTS ??????????????????????????????
    public static class InventoryReportType
    {
        public const string StockSummary = "StockSummary";
        public const string StockLedger = "StockLedger";
        public const string LowStock = "LowStock";
        public const string StockValuation = "StockValuation";
        public const string StockMovement = "StockMovement";
        public const string WarehouseStock = "WarehouseStock";
        public const string TransactionHistory = "TransactionHistory";
        public const string SummaryStats = "SummaryStats";
    }

    // ?? GENERIC REPORT REQUEST ??????????????????????????????
    public class InventoryReportRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public int CompanyID { get; set; }   // from JWT
        public int? WarehouseID { get; set; }
        public int? ItemID { get; set; }
        public int? CategoryID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Status { get; set; }
        public string? SearchText { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortBy { get; set; }
        public string SortOrder { get; set; } = "ASC";
    }

    // ?? RESPONSE DTOs ????????????????????????????????????????

    public class StockSummaryDto
    {
        public int StockID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? ItemDescription { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? WarehouseCode { get; set; }
        public string? UOMName { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityReserved { get; set; }
        public decimal QuantityAvailable { get; set; }
        public decimal StockValue { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal MinimumLevel { get; set; }
        public decimal MaximumLevel { get; set; }
        public decimal SafetyStock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public int TotalRecords { get; set; }
    }

    public class StockLedgerDto
    {
        public int LedgerID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string TransactionNo { get; set; } = string.Empty;
        public string? ReferenceType { get; set; }
        public int? ReferenceID { get; set; }
        public decimal Quantity { get; set; }
        public decimal PreviousQty { get; set; }
        public decimal NewQty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TransactionValue { get; set; }
        public string? BatchNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime TransactionDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
    }

    public class LowStockDto
    {
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? UOMName { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityAvailable { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal MinimumLevel { get; set; }
        public decimal MaximumLevel { get; set; }
        public decimal SafetyStock { get; set; }
        public decimal ReorderQuantity { get; set; }
        public int LeadTime { get; set; }
        public decimal AverageConsumption { get; set; }
        public decimal ShortageQuantity { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
    }

    public class StockValuationDto
    {
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? UOMName { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityAvailable { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AvailableValue { get; set; }
        public decimal? LastTransactionCost { get; set; }
        public DateTime LastUpdated { get; set; }
        public int TotalRecords { get; set; }
    }

    public class StockMovementDto
    {
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? UOMName { get; set; }
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public decimal TotalAdjusted { get; set; }
        public decimal TotalTransferred { get; set; }
        public int TransactionCount { get; set; }
        public DateTime? FirstTransaction { get; set; }
        public DateTime? LastTransaction { get; set; }
        public int TotalRecords { get; set; }
    }

    public class WarehouseStockDto
    {
        public int WarehouseID { get; set; }
        public string? WarehouseCode { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public decimal TotalQuantityOnHand { get; set; }
        public decimal TotalReserved { get; set; }
        public decimal TotalAvailable { get; set; }
        public decimal TotalStockValue { get; set; }
        public int OutOfStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int TotalRecords { get; set; }
    }

    public class TransactionHistoryDto
    {
        public int LedgerID { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionNo { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string? ReferenceType { get; set; }
        public int? ReferenceID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal PreviousQty { get; set; }
        public decimal NewQty { get; set; }
        public decimal NetChange { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TransactionValue { get; set; }
        public string? BatchNumber { get; set; }
        public string? Notes { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
    }

    public class StockSummaryStatsDto
    {
        public int TotalItems { get; set; }
        public int TotalWarehouses { get; set; }
        public decimal TotalQuantityOnHand { get; set; }
        public decimal TotalStockValue { get; set; }
        public int OutOfStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int PendingTransfers { get; set; }
        public int DraftAdjustments { get; set; }
    }

}
