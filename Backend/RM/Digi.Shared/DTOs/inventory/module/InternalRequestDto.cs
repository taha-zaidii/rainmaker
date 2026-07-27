using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.inventory.module
{
    public class InternalRequestDto
    {
        public int IRID { get; set; }
        public string IRNumber { get; set; } = string.Empty;
        public DateTime IRDate { get; set; }
        public int WarehouseID { get; set; }
        public string? WarehouseName { get; set; }
        public int? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }
        public string? IssuedBy { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "Draft";
        public decimal TotalAmount { get; set; }
        public int CompanyID { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int ItemCount { get; set; }
    }

    public class InternalRequestLineItemDto
    {
        public int IRLineItemID { get; set; }
        public int IRID { get; set; }
        public int ItemID { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal? ApprovedQuantity { get; set; }
        public decimal IssuedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? SerialNumber { get; set; }
        public string? Notes { get; set; }
        public int CompanyID { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class InternalRequestDetailDto
    {
        public InternalRequestDto Header { get; set; } = new();
        public List<InternalRequestLineItemDto> LineItems { get; set; } = new();
    }

    public class InternalRequestBulkSaveRequest
    {
        public InternalRequestHeaderDto Header { get; set; } = new();
        public List<InternalRequestLineItemRequestDto> LineItems { get; set; } = new();
    }

    public class InternalRequestHeaderDto
    {
        public int? IRID { get; set; }
        public string IRNumber { get; set; } = string.Empty;
        public DateTime IRDate { get; set; }
        public int WarehouseID { get; set; }
        public int? DepartmentID { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "Draft";
        public decimal TotalAmount { get; set; }
        public int CompanyID { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class InternalRequestLineItemRequestDto
    {
        public int? IRLineItemID { get; set; }
        public int ItemID { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal? ApprovedQuantity { get; set; }
        public decimal IssuedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? SerialNumber { get; set; }
        public string? Notes { get; set; }
        public int CompanyID { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class InternalRequestFilterRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public int CompanyID { get; set; }
        public string? Status { get; set; }
        public int? WarehouseID { get; set; }
        public int? DepartmentID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class InternalRequestApproveRequest
    {
        public int IRID { get; set; }
        public int CompanyID { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
    }

    public class InternalRequestIssueRequest
    {
        public int IRID { get; set; }
        public int CompanyID { get; set; }
        public string IssuedBy { get; set; } = string.Empty;
        public List<InternalRequestIssueLineItemDto>? LineItems { get; set; }
    }

    public class InternalRequestIssueLineItemDto
    {
        public int ItemID { get; set; }
        public decimal IssuedQuantity { get; set; }
    }

    public class InternalRequestRejectRequest
    {
        public int IRID { get; set; }
        public int CompanyID { get; set; }
        public string RejectedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class PurchaseRequestForIRDto
    {
        public int PurchaseRequestID { get; set; }
        public string PRNumber { get; set; }
        public int? WarehouseID { get; set; }
        public int? DepartmentID { get; set; }
        public List<PRItemForIRDto> Items { get; set; } = new();
    }

    public class PRItemForIRDto
    {
        public int PRItemID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal RequestedQuantity { get; set; }   // original PR quantity
        public decimal AvailableStock { get; set; }
        public decimal FinalQuantity { get; set; }        // min(requested, available)
        public bool IsQuantityAdjusted { get; set; }      // <-- differentiation flag (UI is column ko use karega)
    }

    public class StockCheckDto
    {
        public int ItemID { get; set; }
        public int? WarehouseID { get; set; }
        public decimal? QuantityOnHand { get; set; }
        public decimal? QuantityReserved { get; set; }
        public decimal? QuantityAvailable { get; set; }
    }

    public class AdjustPRQtyItemDto
    {
        public int PRItemID { get; set; }
        public decimal NewQuantity { get; set; }
    }
    public class AdjustPRQtyBulkRequest
    {
        public List<AdjustPRQtyItemDto> Items { get; set; } = new();
        public string UpdatedBy { get; set; }
    }
}
