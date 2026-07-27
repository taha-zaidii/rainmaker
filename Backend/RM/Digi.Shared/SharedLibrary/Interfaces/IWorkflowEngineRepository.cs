using Digi.Shared.DTOs.workflow.module;

namespace Digi.Shared.SharedLibrary.Interfaces
{
    public interface IWorkflowEngineRepository
    {
        Task<WorkflowDashboardStatsDto> GetDashboardStatsAsync(int employeeId, int companyId);
        Task<WorkflowPagedDataResponse<ApprovalAssignmentViewDto>> GetPendingApprovalsAsync(
            int employeeId, string? status, int pageNumber, int pageSize);
        Task<RequestDetailResponseDto?> GetAssignmentDetailAsync(long assignmentId, int loggedInEmployeeId);
        Task<RequestDetailResponseDto?> GetRequestDetailForApiAsync(long requestId);
        Task<WorkflowPagedDataResponse<ApprovalRequestViewDto>> GetApprovalRequestsAsync(
            int companyId, int? workflowId, string? status, int? requestingEmployeeId,
            int pageNumber, int pageSize);
        Task<WorkflowActionResultDto> ProcessApprovalActionAsync(
            long assignmentId, string actionCode, string? comments, int performedByEmployeeId);
        Task<SubmitApprovalResponseDto> SubmitForApprovalAsync(CreateApprovalRequestRequest request);
        Task<WorkflowPagedDataResponse<WorkflowDefinitionListItemDto>> GetWorkflowDefinitionsAsync(
            int? companyId, int? requestTypeId, string? searchText, int pageNumber, int pageSize);
        Task<WorkflowDefinitionResponseDto?> GetWorkflowDefinitionByIdAsync(int workflowId);
        Task<int> SaveWorkflowDefinitionAsync(SaveWorkflowDefinitionRequestDto request, int? performedByEmployeeId);
        Task DeleteWorkflowDefinitionAsync(int workflowId);
        Task<List<WorkflowRequestTypeDto>> GetRequestTypesAsync();
    }
}
