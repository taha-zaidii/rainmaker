using Digi.Shared.DTOs.hrm.module;

namespace Digi.Shared.SharedLibrary.Interfaces
{
    public interface IWorkflowService
    {
        Task StartApprovalWorkflowAsync(string formType, int formId, int employeeId, int companyId, string createdBy);
        Task<bool> IsApprovalFlowConfiguredAsync(string formType, int companyId, int? employeeId = null);
        Task<List<ApproverEmailDto>> GetApproverEmailsByWorkflowAsync(string formType, int formID, int companyID);
        Task<WorkflowEmailDispatchResultDto> SendWorkflowEventEmailsAsync(string formType, int formId, int companyId, string triggerEvent, int? workflowId = null, string? actionType = null, string? remarks = null);
    }
    public class WorkflowEmailDispatchResultDto
    {
        public int Attempted { get; set; }
        public int Sent { get; set; }
        public List<string> Messages { get; set; } = new();
    }
}
