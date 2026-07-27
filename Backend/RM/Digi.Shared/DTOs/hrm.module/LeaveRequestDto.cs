using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Digi.Shared.DTOs.hrm.module
{
    public class LeaveCreateDto
    {
        public int EmployeeID { get; set; }
        public int? LeaveTypeID { get; set; }
        public int? DepartmentID { get; set; }
        public int CompanyID { get; set; }
        public string? CompanyName { get; set; }
        public decimal TotalDays { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Reason { get; set; }
        public string? CreatedBy { get; set; }
        public string? Duration { get; set; }
        [JsonIgnore]
        public List<AttachmentDto>? attachments { get; set; }
    }
    public class LeaveUpdateDto 
    {
        public int LeaveRequestID { get; set; } // ✅ Required for update
        public int EmployeeID { get; set; }
        public int LeaveTypeID { get; set; }
        public int CompanyID { get; set; }
        public string? CompanyName { get; set; }
        public int? AttachmentDetailID { get; set; }
        public string? AttachmentURL { get; set; }

        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }
        public string? Duration { get; set; }
        public decimal TotalDays { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }

        [DataType(DataType.Date)]
        public DateTime RequestDate { get; set; }

        public string? UpdateBy { get; set; }

        [JsonIgnore]
        public List<AttachmentDto>? attachments { get; set; }
    }

    public class LeaveDetailDto
    {
        public int LeaveRequestID { get; set; }
        public int EmployeeID { get; set; }
        public int LeaveTypeID { get; set; }
        public string TypeName { get; set; }
        public int CompanyID { get; set; }
        public string Validity { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public DateTime RequestDate { get; set; }
        public string? EmployeeName { get; set; }
        public string? Duration { get; set; }
        public string EmployeeImage { get; set; }
        public int? AttachmentDetailID { get; set; }

        public List<AttachmentDto>? Attachments { get; set; }
    }

    public class LeaveRequestDto
    {
        public int? LeaveRequestID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public string ImageUrl { get; set; }
        public int LeaveTypeID { get; set; }
        public string TypeName { get; set; }
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; }
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public bool? IsApproved { get; set; }
        public DateTime RequestDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? DeletedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        //public string AttachmentDetailJSON { get; set; } 

        public List<AttachmentDto>? AttachmentDetailJSON { get; set; }


    }
    public class GenericSubmittedRequestDto
    {
        public int WorkflowID { get; set; }
        public string FormType { get; set; } = string.Empty;
        public int FormID { get; set; }
        public int StepNumber { get; set; }
        public int FlowID { get; set; }
        public int CompanyID { get; set; }
        public string? Status { get; set; }
        public bool IsApproved { get; set; }
        public bool IsCurrentStep { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? ApproverID { get; set; }
        public string? Remarks { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public int? RequesterID { get; set; }

        public int? EmployeeID { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DesignationName { get; set; }
        public string? DepartmentName { get; set; }
        public string? FilePath { get; set; }

        public string? ApproverName { get; set; }
        public string? ApproverDesignation { get; set; }

        [NewtonsoftJsonIgnore]
        [JsonIgnore]
        public string? RequestDataJson { get; set; }

        [NewtonsoftJsonIgnore]
        [JsonIgnore]
        public string? AttachmentsJson { get; set; }

        public JsonElement? RequestData { get; set; }
        public List<AttachmentDto>? Attachments { get; set; }
    }


    public class ApprovalFlowRequestDto
    {
        public string FormType { get; set; } = "Leave Management";
        public int? FormId { get; set; }
        public int? EmployeeId { get; set; }
        public int? CompanyId { get; set; }
        public int? NavId { get; set; }  // unique identifier for Leave, Loan, etc.
        public string? CreatedBy { get; set; }
    }

    public class LeaveApprovalWorkflowDto
    {
        public int RequestID { get; set; }
        public List<int> ApproverIDs { get; set; }
    }

    public class LeaveCalcDaysRequestDto
    {
        public int CompanyID { get; set; }
        public int EmployeeID { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class LeaveCalcDaysResponseDto
    {
        public int EmployeeID { get; set; }
        public int CompanyID { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public int TotalCalendarDays { get; set; }
        public int WeekendDays { get; set; }
        public int HolidayDays { get; set; }

        public int ChargeableLeaveDays { get; set; }
    }
}
