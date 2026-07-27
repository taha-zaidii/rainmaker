using Digi.Shared.DTOs.workflow.module;

namespace Digi.Shared.SharedLibrary.Interfaces
{
    public interface IWorkflowEngineService
    {
        Task<WorkflowDashboardStatsDto> GetDashboardAsync(int employeeId, int companyId);
        Task<WorkflowPagedDataResponse<ApprovalAssignmentViewDto>> GetPendingApprovalsAsync(
            int employeeId, string? status, int pageNumber, int pageSize);
        Task<RequestDetailResponseDto?> GetAssignmentDetailAsync(long assignmentId, int loggedInEmployeeId);
        Task<RequestDetailResponseDto?> GetRequestDetailAsync(long requestId);
        Task<WorkflowPagedDataResponse<ApprovalRequestViewDto>> GetRequestsAsync(
            int companyId, int? workflowId, string? status, int? requestingEmployeeId,
            int pageNumber, int pageSize);
        Task<WorkflowActionResponseDto> ProcessApprovalAsync(ProcessApprovalActionRequest request);
        Task<SubmitApprovalResponseDto> SubmitForApprovalAsync(CreateApprovalRequestRequest request);
        Task<WorkflowPagedDataResponse<WorkflowDefinitionListItemDto>> GetDefinitionsAsync(
            int? companyId, int? requestTypeId, string? searchText, int pageNumber, int pageSize);
        Task<WorkflowDefinitionResponseDto?> GetDefinitionByIdAsync(int workflowId);
        Task<int> SaveDefinitionAsync(SaveWorkflowDefinitionRequestDto request, int? performedByEmployeeId);
        Task DeleteDefinitionAsync(int workflowId);
        Task<List<WorkflowRequestTypeDto>> GetRequestTypesAsync();
    }
}
