using Dapper;
using Digi.Shared.DTOs.workflow.module;
using Digi.Shared.SharedLibrary.Interfaces;
using System.Data;
using System.Text.Json;

namespace Digi.Shared.SharedLibrary.Repositories
{
    public class WorkflowEngineRepository : IWorkflowEngineRepository
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly IDbConnection _db;

        public WorkflowEngineRepository(IDbConnection db) => _db = db;

        public async Task<WorkflowDashboardStatsDto> GetDashboardStatsAsync(int employeeId, int companyId)
        {
            return await _db.QueryFirstAsync<WorkflowDashboardStatsDto>(
                "[WF].[sp_GetDashboardStats]",
                new { EmployeeID = employeeId, CompanyID = companyId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<WorkflowPagedDataResponse<ApprovalAssignmentViewDto>> GetPendingApprovalsAsync(
            int employeeId, string? status, int pageNumber, int pageSize)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@EmployeeID", employeeId);
            parameters.Add("@Status", status);
            parameters.Add("@PageNumber", pageNumber);
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var data = (await _db.QueryAsync<ApprovalAssignmentViewDto>(
                "[WF].[sp_GetPendingApprovals]", parameters, commandType: CommandType.StoredProcedure)).ToList();

            var total = parameters.Get<int>("@TotalRecords");
            return BuildPage(data, pageNumber, pageSize, total);
        }

        public async Task<RequestDetailResponseDto?> GetAssignmentDetailAsync(long assignmentId, int loggedInEmployeeId)
        {
            using var multi = await _db.QueryMultipleAsync(
                "[WF].[sp_GetApprovalAssignmentDetail]",
                new { AssignmentID = assignmentId, EmployeeID = loggedInEmployeeId },
                commandType: CommandType.StoredProcedure);

            return await ReadRequestDetailAsync(multi, includeAssignment: true);
        }

        public async Task<RequestDetailResponseDto?> GetRequestDetailForApiAsync(long requestId)
        {
            using var multi = await _db.QueryMultipleAsync(
                "[WF].[sp_GetRequestDetail_ForAPI]",
                new { RequestID = requestId },
                commandType: CommandType.StoredProcedure);

            return await ReadRequestDetailAsync(multi, includeAssignment: false);
        }

        public async Task<WorkflowPagedDataResponse<ApprovalRequestViewDto>> GetApprovalRequestsAsync(
            int companyId, int? workflowId, string? status, int? requestingEmployeeId,
            int pageNumber, int pageSize)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CompanyID", companyId);
            parameters.Add("@WorkflowID", workflowId);
            parameters.Add("@Status", status);
            parameters.Add("@RequestingEmployeeID", requestingEmployeeId);
            parameters.Add("@PageNumber", pageNumber);
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var data = (await _db.QueryAsync<ApprovalRequestViewDto>(
                "[WF].[sp_GetApprovalRequests]", parameters, commandType: CommandType.StoredProcedure)).ToList();

            return BuildPage(data, pageNumber, pageSize, parameters.Get<int>("@TotalRecords"));
        }

        public async Task<WorkflowActionResultDto> ProcessApprovalActionAsync(
            long assignmentId, string actionCode, string? comments, int performedByEmployeeId)
        {
            var row = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "[WF].[sp_ProcessApprovalAction]",
                new
                {
                    AssignmentID = assignmentId,
                    ActionCode = actionCode,
                    Comments = comments,
                    PerformedByEmployeeID = performedByEmployeeId
                },
                commandType: CommandType.StoredProcedure);

            if (row == null)
            {
                throw new InvalidOperationException(
                    "Approval action did not return a result. Run migration 006_WF_ProcessApprovalAction_Fix.sql on the database.");
            }

            var d = (IDictionary<string, object>)row;
            return new WorkflowActionResultDto
            {
                RequestID = GetLong(d, "requestID", "RequestID"),
                NewStatus = GetString(d, "newStatus", "NewStatus")
            };
        }

        public async Task<SubmitApprovalResponseDto> SubmitForApprovalAsync(CreateApprovalRequestRequest request)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FormTypeID", request.FormTypeID);
            parameters.Add("@FormRecordID", request.FormRecordID);
            parameters.Add("@RequestingEmployeeID", request.RequestingEmployeeID);
            parameters.Add("@CompanyID", request.CompanyID);
            parameters.Add("@DivisionID", request.DivisionID);
            parameters.Add("@DepartmentID", request.DepartmentID);
            parameters.Add("@RequestTypeID", request.RequestTypeID);
            parameters.Add("@OUT_RequestID", dbType: DbType.Int64, direction: ParameterDirection.Output);
            parameters.Add("@OUT_RequestCode", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);

            await _db.ExecuteAsync("[WF].[sp_SubmitForApproval]", parameters, commandType: CommandType.StoredProcedure);

            return new SubmitApprovalResponseDto
            {
                RequestID = parameters.Get<long>("@OUT_RequestID"),
                RequestCode = parameters.Get<string>("@OUT_RequestCode") ?? string.Empty,
                Status = "IN_PROGRESS",
                Message = "Submitted"
            };
        }

        public async Task<WorkflowPagedDataResponse<WorkflowDefinitionListItemDto>> GetWorkflowDefinitionsAsync(
            int? companyId, int? requestTypeId, string? searchText, int pageNumber, int pageSize)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CompanyID", companyId);
            parameters.Add("@RequestTypeID", requestTypeId);
            parameters.Add("@SearchText", searchText);
            parameters.Add("@PageNumber", pageNumber);
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var rows = (await _db.QueryAsync<dynamic>(
                "[WF].[sp_GetWorkflowDefinitions]", parameters, commandType: CommandType.StoredProcedure)).ToList();

            var data = rows.Select(MapDefinitionListItem).ToList();
            return BuildPage(data, pageNumber, pageSize, parameters.Get<int>("@TotalRecords"));
        }

        public async Task<WorkflowDefinitionResponseDto?> GetWorkflowDefinitionByIdAsync(int workflowId)
        {
            using var multi = await _db.QueryMultipleAsync(
                "[WF].[sp_GetWorkflowDefinitionById]",
                new { WorkflowID = workflowId },
                commandType: CommandType.StoredProcedure);

            var workflowRow = (await multi.ReadAsync<dynamic>()).FirstOrDefault();
            if (workflowRow == null) return null;

            var stepRows = (await multi.ReadAsync<dynamic>()).ToList();
            var actionRows = (await multi.ReadAsync<dynamic>()).ToList();

            var workflow = MapWorkflowSetup(workflowRow);
            var steps = stepRows.Select<dynamic, WorkflowStepUiDto>(s => MapWorkflowStep(s, actionRows)).ToList();

            return new WorkflowDefinitionResponseDto { Workflow = workflow, Steps = steps };
        }

        public async Task<int> SaveWorkflowDefinitionAsync(SaveWorkflowDefinitionRequestDto request, int? performedByEmployeeId)
        {
            var workflowJson = JsonSerializer.Serialize(request.Workflow, JsonOptions);
            var stepsJson = JsonSerializer.Serialize(request.Steps, JsonOptions);

            var parameters = new DynamicParameters();
            parameters.Add("@WorkflowJson", workflowJson);
            parameters.Add("@StepsJson", stepsJson);
            parameters.Add("@PerformedByEmployeeID", performedByEmployeeId);
            parameters.Add("@OUT_WorkflowID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _db.ExecuteAsync("[WF].[sp_SaveWorkflowDefinition]", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("@OUT_WorkflowID");
        }

        public async Task DeleteWorkflowDefinitionAsync(int workflowId)
        {
            await _db.ExecuteAsync(
                "[WF].[sp_DeleteWorkflowDefinition]",
                new { WorkflowID = workflowId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<WorkflowRequestTypeDto>> GetRequestTypesAsync()
        {
            var rows = await _db.QueryAsync<WorkflowRequestTypeDto>(
                "[WF].[sp_GetRequestTypes]",
                commandType: CommandType.StoredProcedure);
            return rows.ToList();
        }

        private static async Task<RequestDetailResponseDto?> ReadRequestDetailAsync(
            SqlMapper.GridReader multi, bool includeAssignment)
        {
            var requestRow = (await multi.ReadAsync<dynamic>()).FirstOrDefault();
            if (requestRow == null) return null;

            var timelineRows = (await multi.ReadAsync<dynamic>()).ToList();
            var summaryFields = (await multi.ReadAsync<WorkflowSummaryFieldDto>()).ToList();

            ApprovalAssignmentViewDto? assignment = null;
            if (includeAssignment && !multi.IsConsumed)
            {
                var asgRow = (await multi.ReadAsync<dynamic>()).FirstOrDefault();
                if (asgRow != null)
                    assignment = MapAssignmentView(asgRow);
            }

            return new RequestDetailResponseDto
            {
                Request = MapRequestHeader(requestRow),
                Timeline = timelineRows.Select(MapTimelineStep).ToList(),
                SummaryFields = summaryFields,
                CurrentAssignment = assignment
            };
        }

        private static ApprovalRequestViewDto MapRequestHeader(dynamic row)
        {
            var d = (IDictionary<string, object>)row;
            return new ApprovalRequestViewDto
            {
                RequestID = GetLong(d, "requestID", "requestId", "RequestID"),
                WorkflowID = GetInt(d, "workflowID", "workflowId", "WorkflowID"),
                FormTypeID = GetInt(d, "formTypeID", "formTypeId", "FormTypeID"),
                FormRecordID = GetLong(d, "formRecordID", "formRecordId", "FormRecordID"),
                RequestCode = GetString(d, "requestCode", "RequestCode"),
                RequestingEmployeeID = GetInt(d, "requestingEmployeeID", "requesterEmployeeId", "RequestingEmployeeID"),
                RequestedDate = GetDate(d, "requestedDate", "RequestedDate") ?? DateTime.MinValue,
                CurrentStepID = GetNullableInt(d, "currentStepID", "currentStepId", "CurrentStepID"),
                Status = GetString(d, "status", "Status"),
                CompanyID = GetInt(d, "companyID", "companyId", "CompanyID"),
                DivisionID = GetNullableInt(d, "divisionID", "divisionId", "DivisionID"),
                DepartmentID = GetNullableInt(d, "departmentID", "departmentId", "DepartmentID"),
                WorkflowName = GetString(d, "workflowName", "WorkflowName"),
                FormTypeName = GetString(d, "formTypeName", "FormTypeName"),
                RequesterName = GetString(d, "requesterName", "RequesterName"),
                RequesterCode = GetString(d, "requesterCode", "requesterEmployeeCode", "RequesterCode"),
                DepartmentName = GetString(d, "departmentName", "DepartmentName"),
                CurrentStepName = GetString(d, "currentStepName", "CurrentStepName"),
                TotalSteps = GetInt(d, "totalSteps", "TotalSteps"),
                CompletedSteps = GetInt(d, "completedSteps", "CompletedSteps"),
                ProgressPercent = GetInt(d, "progressPercent", "ProgressPercent"),
                PendingDays = GetInt(d, "pendingDays", "PendingDays"),
                Priority = Coalesce(GetString(d, "priority", "Priority"), "NORMAL")
            };
        }

        private static WorkflowTimelineStepDto MapTimelineStep(dynamic row)
        {
            var d = (IDictionary<string, object>)row;
            // Legacy 002 audit-event rows (eventType column) — skip empty mapping
            if (d.ContainsKey("eventType") && !d.ContainsKey("stepSequence"))
            {
                return new WorkflowTimelineStepDto
                {
                    StepName = GetString(d, "stepName", "StepName"),
                    Status = GetString(d, "newStatus", "status", "Status"),
                    Comments = GetString(d, "comments", "Comments"),
                    ActionDate = GetDate(d, "eventDate", "actionDate", "ActionDate"),
                    ApproverName = GetString(d, "performedByName", "approverName", "ApproverName")
                };
            }

            return new WorkflowTimelineStepDto
            {
                StepSequence = GetInt(d, "stepSequence", "StepSequence"),
                StepName = GetString(d, "stepName", "StepName"),
                StepCode = GetString(d, "stepCode", "StepCode"),
                Status = GetString(d, "status", "Status"),
                ApproverName = NullIfEmpty(GetString(d, "approverName", "ApproverName")),
                ApproverRole = NullIfEmpty(GetString(d, "approverRole", "ApproverRole")),
                ActionDate = GetDate(d, "actionDate", "ActionDate"),
                Comments = NullIfEmpty(GetString(d, "comments", "Comments")),
                IsCurrent = GetBool(d, "isCurrent", "IsCurrent"),
                SLA_Hours = GetNullableInt(d, "sLA_Hours", "slaHours", "SLA_Hours"),
                DueDate = GetDate(d, "dueDate", "DueDate")
            };
        }

        private static ApprovalAssignmentViewDto MapAssignmentView(dynamic row)
        {
            var d = (IDictionary<string, object>)row;
            return new ApprovalAssignmentViewDto
            {
                AssignmentID = GetLong(d, "assignmentID", "assignmentId", "AssignmentID"),
                RequestID = GetLong(d, "requestID", "requestId", "RequestID"),
                StepID = GetInt(d, "stepID", "stepId", "StepID"),
                AssignedToEmployeeID = GetInt(d, "assignedToEmployeeID", "assignedToEmployeeId", "AssignedToEmployeeID"),
                ApprovalStatus = GetString(d, "approvalStatus", "ApprovalStatus"),
                ApprovalDate = GetDate(d, "approvalDate", "ApprovalDate"),
                Comments = NullIfEmpty(GetString(d, "comments", "Comments")),
                DueDate = GetDate(d, "dueDate", "DueDate"),
                IsOverdue = GetBool(d, "isOverdue", "IsOverdue"),
                RequestCode = GetString(d, "requestCode", "RequestCode"),
                RequesterName = GetString(d, "requesterName", "RequesterName"),
                RequesterCode = GetString(d, "requesterCode", "RequesterCode"),
                RequesterDepartment = GetString(d, "requesterDepartment", "RequesterDepartment"),
                FormTypeName = GetString(d, "formTypeName", "FormTypeName"),
                WorkflowName = GetString(d, "workflowName", "WorkflowName"),
                StepName = GetString(d, "stepName", "StepName"),
                StepSequence = GetInt(d, "stepSequence", "StepSequence"),
                RequestedDate = GetDate(d, "requestedDate", "RequestedDate") ?? DateTime.MinValue,
                Priority = Coalesce(GetString(d, "priority", "Priority"), "NORMAL"),
                TotalSteps = GetInt(d, "totalSteps", "TotalSteps"),
                CurrentStepSequence = GetInt(d, "currentStepSequence", "CurrentStepSequence"),
                AllowReject = GetBool(d, "allowReject", "AllowReject"),
                AllowReturn = GetBool(d, "allowReturn", "AllowReturn"),
                AllowSkip = GetBool(d, "allowSkip", "AllowSkip"),
                RequiresCommentsOnReject = GetBool(d, "requiresCommentsOnReject", "RequiresCommentsOnReject")
            };
        }

        private static string? NullIfEmpty(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;

        private static string Coalesce(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;

        private static long GetLong(IDictionary<string, object> d, params string[] keys)
        {
            foreach (var k in keys)
                if (d.TryGetValue(k, out var v) && v != null && v != DBNull.Value)
                    return Convert.ToInt64(v);
            return 0;
        }

        private static WorkflowPagedDataResponse<T> BuildPage<T>(List<T> data, int pageNumber, int pageSize, int total)
        {
            var pages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0;
            return new WorkflowPagedDataResponse<T>
            {
                Data = data,
                Pagination = new WorkflowPaginationDto
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = total,
                    TotalPages = pages
                }
            };
        }

        private static WorkflowDefinitionListItemDto MapDefinitionListItem(dynamic row)
        {
            var dict = (IDictionary<string, object>)row;
            return new WorkflowDefinitionListItemDto
            {
                WorkflowID = GetInt(dict, "workflowId", "WorkflowID"),
                WorkflowCode = GetString(dict, "workflowCode", "WorkflowCode"),
                WorkflowName = GetString(dict, "workflowName", "WorkflowName"),
                RequestTypeID = GetInt(dict, "requestTypeID", "requestTypeId", "RequestTypeID"),
                RequestTypeCode = GetString(dict, "requestTypeCode", "RequestTypeCode"),
                RequestTypeName = GetString(dict, "requestTypeName", "RequestTypeName"),
                CompanyID = GetInt(dict, "companyId", "CompanyID"),
                DivisionID = GetNullableInt(dict, "divisionId", "DivisionID"),
                DepartmentID = GetNullableInt(dict, "departmentId", "departmentID", "DepartmentID"),
                DepartmentName = GetString(dict, "departmentName", "DepartmentName"),
                GroupCode = GetString(dict, "groupCode", "GroupCode"),
                IsActive = GetBool(dict, "isActive", "isEnabled", "IsActive"),
                Remarks = GetString(dict, "remarks", "Remarks"),
                CreatedDate = GetDate(dict, "createdDate", "CreatedDate"),
                ModifiedDate = GetDate(dict, "modifiedDate", "ModifiedDate")
            };
        }

        private static WorkflowSetupDto MapWorkflowSetup(dynamic row)
        {
            var dict = (IDictionary<string, object>)row;
            return new WorkflowSetupDto
            {
                WorkflowID = GetInt(dict, "workflowID", "workflowId", "WorkflowID"),
                WorkflowCode = GetString(dict, "workflowCode", "WorkflowCode"),
                WorkflowName = GetString(dict, "workflowName", "WorkflowName"),
                RequestTypeID = GetInt(dict, "requestTypeID", "requestTypeId", "RequestTypeID"),
                CompanyID = GetInt(dict, "companyID", "companyId", "CompanyID"),
                DivisionID = GetNullableInt(dict, "divisionID", "divisionId", "DivisionID"),
                DepartmentID = GetNullableInt(dict, "departmentID", "departmentId", "DepartmentID"),
                GroupCode = GetString(dict, "groupCode", "GroupCode"),
                Remarks = GetString(dict, "remarks", "Remarks"),
                IsActive = GetBool(dict, "isEnabled", "isActive", "IsActive")
            };
        }

        private static WorkflowStepUiDto MapWorkflowStep(dynamic stepRow, List<dynamic> allActions)
        {
            var dict = (IDictionary<string, object>)stepRow;
            var stepId = GetInt(dict, "stepId", "StepID");
            var actions = allActions
                .Where(a => GetInt((IDictionary<string, object>)a, "stepId", "StepID") == stepId)
                .Select(a =>
                {
                    var ad = (IDictionary<string, object>)a;
                    return new StepActionDto
                    {
                        ActionID = GetInt(ad, "actionId", "ActionID"),
                        StepID = stepId,
                        ActionCode = GetString(ad, "actionCode", "ActionCode"),
                        ActionName = GetString(ad, "actionName", "ActionName"),
                        NextStepID = GetNullableInt(ad, "nextStepId", "NextStepID"),
                        FinalStatus = GetString(ad, "finalStatus", "FinalStatus"),
                        RequiresComments = GetBool(ad, "requiresComments", "RequiresComments")
                    };
                }).ToList();

            return new WorkflowStepUiDto
            {
                StepID = stepId,
                WorkflowID = GetInt(dict, "workflowId", "WorkflowID"),
                StepSequence = GetInt(dict, "stepSequence", "StepSequence"),
                StepCode = GetString(dict, "stepCode", "StepCode"),
                StepName = GetString(dict, "stepName", "StepName"),
                StepType = GetString(dict, "stepType", "StepType"),
                RoleResolutionType = GetString(dict, "roleResolutionType", "RoleResolutionType"),
                RoleID = GetNullableInt(dict, "roleId", "RoleID"),
                IsPoolApproval = GetBool(dict, "isPoolApproval", "IsPoolApproval"),
                RequiredApprovals = GetInt(dict, "requiredApprovals", "RequiredApprovals"),
                AllowReject = GetBool(dict, "allowReject", "AllowReject"),
                AllowReturn = GetBool(dict, "allowReturn", "AllowReturn"),
                AllowSkip = GetBool(dict, "allowSkip", "AllowSkip"),
                AllowDelegate = GetBool(dict, "allowDelegate", "AllowDelegate"),
                SLA_Hours = GetNullableInt(dict, "slaHours", "SLA_Hours"),
                IsEnabled = GetBool(dict, "isEnabled", "IsActive"),
                Actions = actions
            };
        }

        private static string GetString(IDictionary<string, object> d, params string[] keys)
        {
            foreach (var k in keys)
                if (d.TryGetValue(k, out var v) && v != null && v != DBNull.Value)
                    return v.ToString() ?? string.Empty;
            return string.Empty;
        }

        private static int GetInt(IDictionary<string, object> d, params string[] keys)
        {
            foreach (var k in keys)
                if (d.TryGetValue(k, out var v) && v != null && v != DBNull.Value)
                    return Convert.ToInt32(v);
            return 0;
        }

        private static int? GetNullableInt(IDictionary<string, object> d, params string[] keys)
        {
            foreach (var k in keys)
                if (d.TryGetValue(k, out var v) && v != null && v != DBNull.Value)
                    return Convert.ToInt32(v);
            return null;
        }

        private static bool GetBool(IDictionary<string, object> d, params string[] keys)
        {
            foreach (var k in keys)
                if (d.TryGetValue(k, out var v) && v != null && v != DBNull.Value)
                    return Convert.ToBoolean(v);
            return false;
        }

        private static DateTime? GetDate(IDictionary<string, object> d, params string[] keys)
        {
            foreach (var k in keys)
                if (d.TryGetValue(k, out var v) && v != null && v != DBNull.Value)
                    return Convert.ToDateTime(v);
            return null;
        }
    }
}
