namespace Digi.Shared.DTOs.workflow.module
{
    public class WorkflowSetupDto
    {
        public int WorkflowID { get; set; }
        public string WorkflowCode { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;
        public int RequestTypeID { get; set; }
        public int CompanyID { get; set; }
        public int? DivisionID { get; set; }
        public int? DepartmentID { get; set; }
        public string? GroupCode { get; set; }
        public string? Remarks { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class WorkflowRequestTypeDto
    {
        public int RequestTypeID { get; set; }
        public string RequestTypeCode { get; set; } = string.Empty;
        public string RequestTypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class WorkflowStepDto
    {
        public int StepID { get; set; }
        public int WorkflowID { get; set; }
        public int StepSequence { get; set; }
        public string StepCode { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public string StepType { get; set; } = string.Empty;

        public string RoleResolutionType { get; set; } = string.Empty;
        public int? RoleID { get; set; }
        public string? RoleResolutionFunctionName { get; set; }
        public int? HierarchyLevels { get; set; }
        public bool IsPoolApproval { get; set; }
        public int RequiredApprovals { get; set; }

        public bool AllowReject { get; set; }
        public bool AllowReturn { get; set; }
        public bool AllowSkip { get; set; }
        public bool AllowDelegate { get; set; }
        public int? SLA_Hours { get; set; }

        public List<StepActionDto> Actions { get; set; } = new();
    }

    public class StepActionDto
    {
        public int ActionID { get; set; }
        public int StepID { get; set; }
        public string ActionCode { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public int? NextStepID { get; set; }
        public string? FinalStatus { get; set; }
        public bool RequiresComments { get; set; }
    }

    public class ApprovalRequestDto
    {
        public long RequestID { get; set; }
        public int WorkflowID { get; set; }
        public int FormTypeID { get; set; }
        public long FormRecordID { get; set; }

        public string? RequestCode { get; set; }
        public int RequestingEmployeeID { get; set; }
        public DateTime RequestedDate { get; set; }
        public int? CurrentStepID { get; set; }

        public string Status { get; set; } = string.Empty;
        public int CompanyID { get; set; }
        public int? DivisionID { get; set; }
        public int? DepartmentID { get; set; }

        public DateTime? SubmittedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    public class ApprovalRequestListDto : ApprovalRequestDto
    {
        public string? FormTypeName { get; set; }
        public string? StepName { get; set; }
        public int PendingCount { get; set; }
    }

    public class PendingApprovalDto
    {
        public long RequestID { get; set; }
        public long AssignmentID { get; set; }
        public string? RequestCode { get; set; }
        public string? FormTypeName { get; set; }
        public int RequestingEmployeeID { get; set; }
        public DateTime RequestedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public string? StepName { get; set; }
        public int StepSequence { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public int CompanyID { get; set; }
        public int? DivisionID { get; set; }
        public int? DepartmentID { get; set; }
    }

    internal class ApprovalRequestDetailRowDto
    {
        public long RequestID { get; set; }
        public int WorkflowID { get; set; }
        public int FormTypeID { get; set; }
        public long FormRecordID { get; set; }
        public string? RequestCode { get; set; }
        public string Status { get; set; } = string.Empty;
        public int RequestingEmployeeID { get; set; }
        public DateTime RequestedDate { get; set; }
        public int? CurrentStepID { get; set; }
        public int CompanyID { get; set; }
        public int? DivisionID { get; set; }
        public int? DepartmentID { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int? StepID { get; set; }
        public string? StepName { get; set; }
        public int? StepSequence { get; set; }
        public string? StepCode { get; set; }
        public string? StepType { get; set; }
        public string? RoleResolutionType { get; set; }
        public int? RoleID { get; set; }
        public bool? IsPoolApproval { get; set; }
        public int? RequiredApprovals { get; set; }
        public bool? AllowReject { get; set; }
        public bool? AllowReturn { get; set; }
        public bool? AllowSkip { get; set; }
        public bool? AllowDelegate { get; set; }
        public int? SLA_Hours { get; set; }
        public int? ActionID { get; set; }
        public string? ActionCode { get; set; }
        public string? ActionName { get; set; }
        public bool? RequiresComments { get; set; }
        public int? NextStepID { get; set; }
        public string? FinalStatus { get; set; }
    }

    public class ApprovalAssignmentDto
    {
        public long AssignmentID { get; set; }
        public long RequestID { get; set; }
        public int StepID { get; set; }

        public int AssignedToEmployeeID { get; set; }
        public int AssignedByEmployeeID { get; set; }
        public DateTime AssignedDate { get; set; }

        public int? DelegatedToEmployeeID { get; set; }
        public string? DelegationReason { get; set; }
        public DateTime? DelegationEndDate { get; set; }

        public string ApprovalStatus { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
        public string? Comments { get; set; }

        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class ApprovalRequestDetailDto
    {
        public ApprovalRequestDto? Request { get; set; }
        public WorkflowStepDto? CurrentStep { get; set; }
        public List<ApprovalAssignmentDto> Assignments { get; set; } = new();
        public List<ApprovalAuditDto> AuditTrail { get; set; } = new();
        public object? FormPayload { get; set; }
    }

    public class ApprovalAuditDto
    {
        public long AuditID { get; set; }
        public long RequestID { get; set; }
        public long? AssignmentID { get; set; }

        public string Action { get; set; } = string.Empty;
        public int PerformedBy { get; set; }
        public DateTime PerformedDate { get; set; }
        public string? Comments { get; set; }

        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
    }

    public class ProcessApprovalActionRequest
    {
        public long AssignmentID { get; set; }
        public string ActionCode { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public int PerformedByEmployeeID { get; set; }
    }

    public class CreateApprovalRequestRequest
    {
        public int FormTypeID { get; set; }
        public long FormRecordID { get; set; }
        public int RequestingEmployeeID { get; set; }
        public int CompanyID { get; set; }
        public int? DivisionID { get; set; }
        public int? DepartmentID { get; set; }
        public int RequestTypeID { get; set; }
    }

    public class WorkflowPaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (TotalRecords + PageSize - 1) / PageSize : 0;
    }
}
