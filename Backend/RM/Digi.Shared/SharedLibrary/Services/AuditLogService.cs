using Dapper;
using Digi.Shared.Helper;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Security.Claims;

namespace Digi.Shared.SharedLibrary.Services
{
    /// <summary>
    /// Generic Audit Log Service Implementation using Dapper
    /// Logs all actions across all modules to database using stored procedure
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly string _connectionString;
        private readonly ILogger<AuditLogService> _logger;
        private readonly int _commandTimeout;
        private const string StoredProcedureName = "sp_Sys_AuditLog_Insert";

        public AuditLogService(IConfiguration configuration, ILogger<AuditLogService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                throw new ArgumentNullException("DefaultConnection string is missing in configuration");
            _logger = logger;
            _commandTimeout = configuration.GetValue<int?>("Database:CommandTimeout") ?? 30;
        }

        public async Task LogActionAsync(
            string module,
            string? controller = null,
            string? action = null,
            string? httpMethod = null,
            string? requestUrl = null,
            ClaimsPrincipal? user = null,
            string? ipAddress = null,
            string? machineName = null,
            string? actionType = null,
            string? entityName = null,
            string? entityId = null,
            string? oldValues = null,
            string? newValues = null,
            string? description = null,
            string? status = "Success",
            string? errorMessage = null,
            long? durationMs = null,
            string? userAgent = null)
        {
            try
            {
                var model = new AuditLogModel
                {
                    Module = module,
                    Controller = controller,
                    Action = action,
                    HttpMethod = httpMethod,
                    RequestUrl = requestUrl,
                    User = user,
                    IpAddress = ipAddress,
                    MachineName = machineName,
                    ActionType = actionType,
                    EntityName = entityName,
                    EntityId = entityId,
                    OldValues = oldValues,
                    NewValues = newValues,
                    Description = description,
                    Status = status,
                    ErrorMessage = errorMessage,
                    DurationMs = durationMs,
                    UserAgent = userAgent
                };

                await LogActionAsync(model);
            }
            catch (Exception ex)
            {
                // Don't throw exception for audit log failures - just log it
                _logger.LogError(ex, "Failed to log audit action. Module: {Module}, Action: {Action}", module, action);
            }
        }

        public async Task LogActionAsync(AuditLogModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Module))
                {
                    _logger.LogWarning("Audit log skipped: Module name is required");
                    return;
                }

                // Extract user information from ClaimsPrincipal
                int? userId = null;
                string? userName = null;
                int? companyId = null;
                int? employeeId = null;

                if (model.User != null)
                {
                    userId = model.User.GetUserId();
                    userName = model.User.FindFirst("UserName")?.Value
                        ?? model.User.FindFirst(ClaimTypes.Name)?.Value
                        ?? model.User.Identity?.Name;
                    companyId = model.User.GetCompanyId();
                    employeeId = model.User.GetEmployeeId();
                }

                // Prepare parameters for stored procedure
                var parameters = new DynamicParameters();
                parameters.Add("@Module", model.Module, DbType.String);
                parameters.Add("@Controller", model.Controller, DbType.String);
                parameters.Add("@Action", model.Action, DbType.String);
                parameters.Add("@HttpMethod", model.HttpMethod, DbType.String);
                parameters.Add("@RequestUrl", model.RequestUrl, DbType.String);
                parameters.Add("@UserId", userId, DbType.Int32);
                parameters.Add("@UserName", userName, DbType.String);
                parameters.Add("@CompanyId", companyId, DbType.Int32);
                parameters.Add("@EmployeeId", employeeId, DbType.Int32);
                parameters.Add("@IpAddress", model.IpAddress, DbType.String);
                parameters.Add("@MachineName", model.MachineName, DbType.String);
                parameters.Add("@ActionType", model.ActionType, DbType.String);
                parameters.Add("@EntityName", model.EntityName, DbType.String);
                parameters.Add("@EntityId", model.EntityId, DbType.String);
                
                // OldValues - Ensure it's passed even if null
                parameters.Add("@OldValues", 
                    string.IsNullOrWhiteSpace(model.OldValues) ? null : model.OldValues, 
                    DbType.String, 
                    size: int.MaxValue);
                
                // NewValues - Ensure it's passed even if null
                parameters.Add("@NewValues", 
                    string.IsNullOrWhiteSpace(model.NewValues) ? null : model.NewValues, 
                    DbType.String, 
                    size: int.MaxValue);
                
                parameters.Add("@Description", model.Description, DbType.String);
                parameters.Add("@Status", model.Status ?? "Success", DbType.String);
                parameters.Add("@ErrorMessage", model.ErrorMessage, DbType.String);
                parameters.Add("@DurationMs", model.DurationMs, DbType.Int64);
                parameters.Add("@UserAgent", model.UserAgent, DbType.String);
                parameters.Add("@ReturnCode", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                // Debug logging (only in development)
                if (!string.IsNullOrWhiteSpace(model.OldValues) || !string.IsNullOrWhiteSpace(model.NewValues))
                {
                    _logger.LogDebug("Audit Log - OldValues Length: {OldLength}, NewValues Length: {NewLength}", 
                        model.OldValues?.Length ?? 0, 
                        model.NewValues?.Length ?? 0);
                }

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                await connection.ExecuteAsync(
                    StoredProcedureName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout
                );

                var returnCode = parameters.Get<int>("@ReturnCode");
                if (returnCode < 0)
                {
                    _logger.LogWarning("Audit log stored procedure returned error code: {ReturnCode}", returnCode);
                }
            }
            catch (Exception ex)
            {
                // Don't throw exception for audit log failures - just log it
                _logger.LogError(ex, "Failed to log audit action. Module: {Module}, Action: {Action}", 
                    model.Module, model.Action);
            }
        }

        public async Task LogSuccessAsync(
            string module,
            string? controller = null,
            string? action = null,
            string? httpMethod = null,
            string? requestUrl = null,
            ClaimsPrincipal? user = null,
            string? ipAddress = null,
            string? machineName = null,
            string? actionType = null,
            string? entityName = null,
            string? entityId = null,
            string? description = null,
            long? durationMs = null)
        {
            await LogActionAsync(
                module: module,
                controller: controller,
                action: action,
                httpMethod: httpMethod,
                requestUrl: requestUrl,
                user: user,
                ipAddress: ipAddress,
                machineName: machineName,
                actionType: actionType,
                entityName: entityName,
                entityId: entityId,
                description: description,
                status: "Success",
                durationMs: durationMs);
        }

        public async Task LogFailureAsync(
            string module,
            string? controller = null,
            string? action = null,
            string? httpMethod = null,
            string? requestUrl = null,
            ClaimsPrincipal? user = null,
            string? ipAddress = null,
            string? machineName = null,
            string? actionType = null,
            string? entityName = null,
            string? entityId = null,
            string? errorMessage = null,
            string? description = null)
        {
            await LogActionAsync(
                module: module,
                controller: controller,
                action: action,
                httpMethod: httpMethod,
                requestUrl: requestUrl,
                user: user,
                ipAddress: ipAddress,
                machineName: machineName,
                actionType: actionType,
                entityName: entityName,
                entityId: entityId,
                description: description,
                status: "Failed",
                errorMessage: errorMessage);
        }
    }
}

