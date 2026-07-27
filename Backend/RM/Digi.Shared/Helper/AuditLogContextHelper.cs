using Microsoft.AspNetCore.Http;

namespace Digi.Shared.Helper
{
    /// <summary>
    /// Helper class to set audit context values in HttpContext.
    /// Call SetOldValues in service layer before Update/Delete operations.
    /// </summary>
    public static class AuditLogContextHelper
    {
        private const string OldValuesKey = "AuditLog_OldValues";
        private const string EntityIdKey = "AuditLog_EntityId";
        private const string ActionTypeKey = "AuditLog_ActionType";

        /// <summary>
        /// Set old values in HttpContext for audit logging.
        /// Call this in service layer before update/delete operation.
        /// </summary>
        public static void SetOldValues(HttpContext? httpContext, object? oldValues)
        {
            if (httpContext == null || oldValues == null) return;

            try
            {
                var serialized = AuditLogHelper.SerializeForAudit(oldValues);
                if (!string.IsNullOrWhiteSpace(serialized))
                    httpContext.Items[OldValuesKey] = serialized;
            }
            catch
            {
                // Ignore errors - audit must not break the request
            }
        }

        /// <summary>
        /// Set entity id in HttpContext (useful after Create when id is generated).
        /// </summary>
        public static void SetEntityId(HttpContext? httpContext, object? entityId)
        {
            if (httpContext == null || entityId == null) return;

            try
            {
                var id = entityId.ToString();
                if (!string.IsNullOrWhiteSpace(id))
                    httpContext.Items[EntityIdKey] = id;
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// Override action type dynamically (e.g. Create vs Update in upsert endpoints).
        /// </summary>
        public static void SetActionType(HttpContext? httpContext, string? actionType)
        {
            if (httpContext == null || string.IsNullOrWhiteSpace(actionType)) return;
            httpContext.Items[ActionTypeKey] = actionType;
        }

        /// <summary>
        /// Get old values from HttpContext.
        /// </summary>
        public static string? GetOldValues(HttpContext? httpContext)
        {
            if (httpContext == null) return null;

            if (httpContext.Items.TryGetValue(OldValuesKey, out var oldValuesObj) && oldValuesObj != null)
                return oldValuesObj.ToString();

            return null;
        }

        /// <summary>
        /// Get entity id from HttpContext.
        /// </summary>
        public static string? GetEntityId(HttpContext? httpContext)
        {
            if (httpContext == null) return null;

            if (httpContext.Items.TryGetValue(EntityIdKey, out var entityIdObj) && entityIdObj != null)
                return entityIdObj.ToString();

            return null;
        }
    }
}
