using Digi.Shared.SharedLibrary.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace Digi.Shared.Helper
{
    /// <summary>
    /// Helper class for easy audit logging
    /// </summary>
    public static class AuditLogHelper
    {
        /// <summary>
        /// Serialize object to JSON for audit log
        /// </summary>
        public static string? SerializeForAudit(object? obj)
        {
            if (obj == null) return null;

            try
            {
                return JsonSerializer.Serialize(obj, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    MaxDepth = 10 // Limit depth to prevent circular references
                });
            }
            catch
            {
                return obj.ToString();
            }
        }

        /// <summary>
        /// Create a simple audit log entry
        /// </summary>
        public static async Task LogSimpleAsync(
            IAuditLogService auditLogService,
            string module,
            string actionType,
            string? entityName = null,
            string? entityId = null,
            ClaimsPrincipal? user = null,
            string? description = null)
        {
            if (auditLogService == null) return;

            await auditLogService.LogActionAsync(
                module: module,
                actionType: actionType,
                entityName: entityName,
                entityId: entityId,
                user: user,
                description: description,
                status: "Success");
        }

        /// <summary>
        /// Log update operation with old and new values
        /// </summary>
        public static async Task LogUpdateAsync(
            IAuditLogService auditLogService,
            string module,
            string? controller,
            string? action,
            string? httpMethod,
            ClaimsPrincipal? user,
            string? entityName,
            string? entityId,
            object? oldValues,
            object? newValues,
            string? description = null,
            long? durationMs = null)
        {
            if (auditLogService == null) return;

            var oldValuesJson = SerializeForAudit(oldValues);
            var newValuesJson = SerializeForAudit(newValues);

            await auditLogService.LogActionAsync(
                module: module,
                controller: controller,
                action: action,
                httpMethod: httpMethod ?? "PUT",
                user: user,
                actionType: "Update",
                entityName: entityName,
                entityId: entityId,
                oldValues: oldValuesJson,
                newValues: newValuesJson,
                description: description,
                status: "Success",
                durationMs: durationMs);
        }

        /// <summary>
        /// Log create operation with new values
        /// </summary>
        public static async Task LogCreateAsync(
            IAuditLogService auditLogService,
            string module,
            string? controller,
            string? action,
            string? httpMethod,
            ClaimsPrincipal? user,
            string? entityName,
            string? entityId,
            object? newValues,
            string? description = null,
            long? durationMs = null)
        {
            if (auditLogService == null) return;

            var newValuesJson = SerializeForAudit(newValues);

            await auditLogService.LogActionAsync(
                module: module,
                controller: controller,
                action: action,
                httpMethod: httpMethod ?? "POST",
                user: user,
                actionType: "Create",
                entityName: entityName,
                entityId: entityId,
                oldValues: null,
                newValues: newValuesJson,
                description: description,
                status: "Success",
                durationMs: durationMs);
        }

        /// <summary>
        /// Log delete operation with old values
        /// </summary>
        public static async Task LogDeleteAsync(
            IAuditLogService auditLogService,
            string module,
            string? controller,
            string? action,
            string? httpMethod,
            ClaimsPrincipal? user,
            string? entityName,
            string? entityId,
            object? oldValues,
            string? description = null,
            long? durationMs = null)
        {
            if (auditLogService == null) return;

            var oldValuesJson = SerializeForAudit(oldValues);

            await auditLogService.LogActionAsync(
                module: module,
                controller: controller,
                action: action,
                httpMethod: httpMethod ?? "DELETE",
                user: user,
                actionType: "Delete",
                entityName: entityName,
                entityId: entityId,
                oldValues: oldValuesJson,
                newValues: null,
                description: description,
                status: "Success",
                durationMs: durationMs);
        }
    }
}

