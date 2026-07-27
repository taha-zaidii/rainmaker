using Digi.Shared.DTOs.workflow.module;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.Extensions.Logging;

namespace Digi.Shared.SharedLibrary.Services
{
    public class WorkflowEngineService : IWorkflowEngineService
    {
        private readonly IWorkflowEngineRepository _repository;
        private readonly ILogger<WorkflowEngineService> _logger;

        public WorkflowEngineService(IWorkflowEngineRepository repository, ILogger<WorkflowEngineService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<WorkflowDashboardStatsDto> GetDashboardAsync(int employeeId, int companyId) =>
            _repository.GetDashboardStatsAsync(employeeId, companyId);

        public Task<WorkflowPagedDataResponse<ApprovalAssignmentViewDto>> GetPendingApprovalsAsync(
            int employeeId, string? status, int pageNumber, int pageSize) =>
            _repository.GetPendingApprovalsAsync(employeeId, status, pageNumber, pageSize);

        public Task<RequestDetailResponseDto?> GetAssignmentDetailAsync(long assignmentId, int loggedInEmployeeId) =>
            _repository.GetAssignmentDetailAsync(assignmentId, loggedInEmployeeId);

        public Task<RequestDetailResponseDto?> GetRequestDetailAsync(long requestId) =>
            _repository.GetRequestDetailForApiAsync(requestId);

        public Task<WorkflowPagedDataResponse<ApprovalRequestViewDto>> GetRequestsAsync(
            int companyId, int? workflowId, string? status, int? requestingEmployeeId,
            int pageNumber, int pageSize) =>
            _repository.GetApprovalRequestsAsync(companyId, workflowId, status, requestingEmployeeId, pageNumber, pageSize);

        public async Task<WorkflowActionResponseDto> ProcessApprovalAsync(ProcessApprovalActionRequest request)
        {
            var result = await _repository.ProcessApprovalActionAsync(
                request.AssignmentID, request.ActionCode, request.Comments, request.PerformedByEmployeeID);

            return new WorkflowActionResponseDto
            {
                Success = true,
                Message = "Approval action processed successfully",
                Data = result
            };
        }

        public Task<SubmitApprovalResponseDto> SubmitForApprovalAsync(CreateApprovalRequestRequest request) =>
            _repository.SubmitForApprovalAsync(request);

        public Task<WorkflowPagedDataResponse<WorkflowDefinitionListItemDto>> GetDefinitionsAsync(
            int? companyId, int? requestTypeId, string? searchText, int pageNumber, int pageSize) =>
            _repository.GetWorkflowDefinitionsAsync(companyId, requestTypeId, searchText, pageNumber, pageSize);

        public Task<WorkflowDefinitionResponseDto?> GetDefinitionByIdAsync(int workflowId) =>
            _repository.GetWorkflowDefinitionByIdAsync(workflowId);

        public Task<int> SaveDefinitionAsync(SaveWorkflowDefinitionRequestDto request, int? performedByEmployeeId) =>
            _repository.SaveWorkflowDefinitionAsync(request, performedByEmployeeId);

        public Task DeleteDefinitionAsync(int workflowId) =>
            _repository.DeleteWorkflowDefinitionAsync(workflowId);

        public Task<List<WorkflowRequestTypeDto>> GetRequestTypesAsync() =>
            _repository.GetRequestTypesAsync();
    }
}
