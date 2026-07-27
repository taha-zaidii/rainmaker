using System.Text.Json.Serialization;

namespace Digi.Shared.DTOs.workflow.module
{
    public class WorkflowDashboardStatsDto
    {
        public int PendingApprovals { get; set; }
        public int OverdueApprovals { get; set; }
        public int ApprovedToday { get; set; }
        public int MyPendingRequests { get; set; }
        public int MyInProgressRequests { get; set; }
        public int MyCompletedRequests { get; set; }
        public decimal? AvgApprovalHours { get; set; }
    }

    public class WorkflowPaginationDto
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }

    public class WorkflowPagedDataResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public WorkflowPaginationDto Pagination { get; set; } = new();
    }

    public class ApprovalAssignmentViewDto
    {
        public long AssignmentID { get; set; }
        public long RequestID { get; set; }
        public int StepID { get; set; }
        public int AssignedToEmployeeID { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
        public string? Comments { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public string? RequestCode { get; set; }
        public string? RequesterName { get; set; }
        public string? RequesterCode { get; set; }
        public string? RequesterDepartment { get; set; }
        public string? FormTypeName { get; set; }
        public string? WorkflowName { get; set; }
        public string? StepName { get; set; }
        public int StepSequence { get; set; }
        public DateTime RequestedDate { get; set; }
        public string Priority { get; set; } = "NORMAL";
        public int TotalSteps { get; set; }
        public int CurrentStepSequence { get; set; }
        public bool AllowReject { get; set; }
        public bool AllowReturn { get; set; }
        public bool AllowSkip { get; set; }
        public bool RequiresCommentsOnReject { get; set; }
    }

    public class ApprovalRequestViewDto
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
        public string? WorkflowName { get; set; }
        public string? FormTypeName { get; set; }
        public string? RequesterName { get; set; }
        public string? RequesterCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? CurrentStepName { get; set; }
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public int ProgressPercent { get; set; }
        public int PendingDays { get; set; }
        public string Priority { get; set; } = "NORMAL";
    }

    public class WorkflowTimelineStepDto
    {
        public int StepSequence { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string StepCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ApproverName { get; set; }
        public string? ApproverRole { get; set; }
        public DateTime? ActionDate { get; set; }
        public string? Comments { get; set; }
        public bool IsCurrent { get; set; }

        [JsonPropertyName("sLA_Hours")]
        public int? SLA_Hours { get; set; }

        public DateTime? DueDate { get; set; }
    }

    public class WorkflowSummaryFieldDto
    {
        public string Label { get; set; } = string.Empty;
        public string? Value { get; set; }
        public bool Highlight { get; set; }
        public string? Icon { get; set; }
    }

    public class RequestDetailResponseDto
    {
        public ApprovalRequestViewDto? Request { get; set; }
        public List<WorkflowTimelineStepDto> Timeline { get; set; } = new();
        public List<WorkflowSummaryFieldDto> SummaryFields { get; set; } = new();
        public ApprovalAssignmentViewDto? CurrentAssignment { get; set; }
    }

    public class WorkflowActionResultDto
    {
        public long RequestID { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }

    public class WorkflowActionResponseDto
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public WorkflowActionResultDto? Data { get; set; }
    }

    public class SubmitApprovalResponseDto
    {
        public long RequestID { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class WorkflowStepUiDto
    {
        public int StepID { get; set; }
        public int WorkflowID { get; set; }
        public int StepSequence { get; set; }
        public string StepCode { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public string StepType { get; set; } = string.Empty;
        public string RoleResolutionType { get; set; } = string.Empty;
        public int? RoleID { get; set; }
        public bool IsPoolApproval { get; set; }
        public int RequiredApprovals { get; set; }
        public bool AllowReject { get; set; }
        public bool AllowReturn { get; set; }
        public bool AllowSkip { get; set; }
        public bool AllowDelegate { get; set; }

        [JsonPropertyName("sLA_Hours")]
        public int? SLA_Hours { get; set; }

        public bool IsEnabled { get; set; }
        public List<StepActionDto> Actions { get; set; } = new();
    }

    public class WorkflowDefinitionResponseDto
    {
        public WorkflowSetupDto? Workflow { get; set; }
        public List<WorkflowStepUiDto> Steps { get; set; } = new();
    }

    public class SaveWorkflowDefinitionRequestDto
    {
        public WorkflowSetupDto Workflow { get; set; } = new();
        public List<WorkflowStepUiDto> Steps { get; set; } = new();
    }

    public class WorkflowDefinitionListItemDto
    {
        public int WorkflowID { get; set; }
        public string WorkflowCode { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;
        public int RequestTypeID { get; set; }
        public string? RequestTypeCode { get; set; }
        public string? RequestTypeName { get; set; }
        public int CompanyID { get; set; }
        public int? DivisionID { get; set; }
        public int? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public string? GroupCode { get; set; }
        public bool IsActive { get; set; }
        public string? Remarks { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
