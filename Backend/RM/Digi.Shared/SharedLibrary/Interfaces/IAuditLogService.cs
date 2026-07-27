using Digi.Shared.DTOs;
using System.Security.Claims;

namespace Digi.Shared.SharedLibrary.Interfaces
{
    /// <summary>
    /// Generic Audit Log Service Interface
    /// Used for logging all actions across all modules
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Log an action asynchronously
        /// </summary>
        /// <param name="module">Module name (e.g., "HRM", "Sales", "Admin")</param>
        /// <param name="controller">Controller name</param>
        /// <param name="action">Action name (method name)</param>
        /// <param name="httpMethod">HTTP Method</param>
        /// <param name="requestUrl">Request URL</param>
        /// <param name="user">ClaimsPrincipal for user information</param>
        /// <param name="ipAddress">IP Address</param>
        /// <param name="machineName"> MachineName</param>
        /// <param name="actionType">Action type (Create, Read, Update, Delete, etc.)</param>
        /// <param name="entityName">Entity/Table name</param>
        /// <param name="entityId">Entity ID</param>
        /// <param name="oldValues">Old values (JSON) - for Update/Delete</param>
        /// <param name="newValues">New values (JSON) - for Create/Update</param>
        /// <param name="description">Additional description</param>
        /// <param name="status">Status (Success, Failed, Error)</param>
        /// <param name="errorMessage">Error message if failed</param>
        /// <param name="durationMs">Request duration in milliseconds</param>
        /// <param name="userAgent">User Agent string</param>
        /// <returns>Task representing the async operation</returns>
        Task LogActionAsync(
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
            string? userAgent = null);

        /// <summary>
        /// Log an action with a simple model
        /// </summary>
        Task LogActionAsync(AuditLogModel model);

        /// <summary>
        /// Log a successful action
        /// </summary>
        Task LogSuccessAsync(
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
            long? durationMs = null);

        /// <summary>
        /// Log a failed action
        /// </summary>
        Task LogFailureAsync(
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
            string? description = null);
    }

    /// <summary>
    /// Audit Log Model for passing data
    /// </summary>
    public class AuditLogModel
    {
        public string Module { get; set; } = null!;
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public string? HttpMethod { get; set; }
        public string? RequestUrl { get; set; }
        public ClaimsPrincipal? User { get; set; }
        public string? IpAddress { get; set; }
        public string? MachineName { get; set; }
        public string? ActionType { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; } = "Success";
        public string? ErrorMessage { get; set; }
        public long? DurationMs { get; set; }
        public string? UserAgent { get; set; }
    }
}

