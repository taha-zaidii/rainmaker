using Digi.Shared.Helper;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace Digi.Shared.Attributes
{
    /// <summary>
    /// Attribute to automatically log actions to audit log
    /// Usage: [AuditLog("HRM", "Create", "Employee")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class AuditLogAttribute : ActionFilterAttribute
    {
        private readonly string _module;
        private readonly string? _actionType;
        private readonly string? _entityName;
        private Stopwatch? _stopwatch;

        /// <summary>
        /// Initialize AuditLog attribute
        /// </summary>
        /// <param name="module">Module name (e.g., "HRM", "Sales", "Admin")</param>
        /// <param name="actionType">Action type (Create, Read, Update, Delete, etc.) - optional, will be inferred from HTTP method if not provided</param>
        /// <param name="entityName">Entity/Table name - optional</param>
        public AuditLogAttribute(string module, string? actionType = null, string? entityName = null)
        {
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _actionType = actionType;
            _entityName = entityName;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _stopwatch = Stopwatch.StartNew();

            try
            {
                // Capture entity id early from route/arguments (Update/Delete)
                var earlyEntityId = ExtractEntityIdFromRouteOrArgs(context.RouteData, context.ActionArguments);
                if (!string.IsNullOrWhiteSpace(earlyEntityId))
                    AuditLogContextHelper.SetEntityId(context.HttpContext, earlyEntityId);

                // Capture action arguments (request body DTOs) as NewValues
                if (context.ActionArguments != null && context.ActionArguments.Count > 0)
                {
                    var requestData = new Dictionary<string, object?>();
                    foreach (var arg in context.ActionArguments)
                    {
                        // Skip infrastructure args; keep body/query DTOs and ids that are useful in NewValues payload
                        if (arg.Key.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            continue;

                        try
                        {
                            requestData[arg.Key] = arg.Value;
                        }
                        catch
                        {
                            // Ignore serialization errors
                        }
                    }

                    if (requestData.Count > 0)
                    {
                        var serializedRequest = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                            MaxDepth = 10
                        });
                        context.HttpContext.Items["AuditLog_RequestData"] = serializedRequest;
                    }
                }
            }
            catch
            {
                // Ignore errors - don't break the request
            }

            base.OnActionExecuting(context);
        }

        public override async void OnActionExecuted(ActionExecutedContext context)
        {
            _stopwatch?.Stop();

            try
            {
                var auditLogService = context.HttpContext.RequestServices.GetService<IAuditLogService>();
                if (auditLogService == null)
                    return;

                var controllerName = context.RouteData.Values["controller"]?.ToString();
                var actionName = context.RouteData.Values["action"]?.ToString();
                var httpMethod = context.HttpContext.Request.Method;
                var requestUrl = $"{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}{context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}";
                var ipAddress = GetIpAddress(context.HttpContext);
                var machineName = GetClientMachineName(context.HttpContext);
                var userAgent = context.HttpContext.Request.Headers["User-Agent"].ToString();
                var user = context.HttpContext.User;

                // Determine action type if not provided
                string? dynamicActionType = null;
                if (context.HttpContext.Items.TryGetValue("AuditLog_ActionType", out var actionTypeObj) && actionTypeObj != null)
                    dynamicActionType = actionTypeObj.ToString();

                var actionType = _actionType ?? dynamicActionType ?? InferActionType(httpMethod);

                // Entity id: Items (service/Create) → route/args → response Data
                var entityId = AuditLogContextHelper.GetEntityId(context.HttpContext)
                    ?? ExtractEntityIdFromRouteOrArgs(context.RouteData, null)
                    ?? ExtractEntityIdFromResult(context.Result);

                // NewValues from request body captured in OnActionExecuting
                string? newValues = null;
                if (context.HttpContext.Items.TryGetValue("AuditLog_RequestData", out var requestDataObj) && requestDataObj != null)
                {
                    try { newValues = requestDataObj.ToString(); }
                    catch { /* ignore */ }
                }

                // OldValues: must be set by service via AuditLogContextHelper.SetOldValues before write
                var oldValues = AuditLogContextHelper.GetOldValues(context.HttpContext);

                // Create has no prior state
                if (string.Equals(actionType, "Create", StringComparison.OrdinalIgnoreCase))
                    oldValues = null;

                var status = "Success";
                string? errorMessage = null;

                if (context.Exception != null)
                {
                    status = "Error";
                    errorMessage = context.Exception.Message;
                }
                else if (context.Result is ObjectResult result)
                {
                    if (result.StatusCode >= 400)
                    {
                        status = "Failed";
                        if (result.Value != null)
                        {
                            try { errorMessage = JsonSerializer.Serialize(result.Value); }
                            catch { errorMessage = result.Value.ToString(); }
                        }
                    }
                }

                await auditLogService.LogActionAsync(
                    module: _module,
                    controller: controllerName,
                    action: actionName,
                    httpMethod: httpMethod,
                    requestUrl: requestUrl,
                    user: user,
                    ipAddress: ipAddress,
                    machineName: machineName,
                    actionType: actionType,
                    entityName: _entityName,
                    entityId: entityId,
                    oldValues: oldValues,
                    newValues: newValues,
                    description: null,
                    status: status,
                    errorMessage: errorMessage,
                    durationMs: _stopwatch?.ElapsedMilliseconds,
                    userAgent: userAgent);
            }
            catch (Exception ex)
            {
                try
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<AuditLogAttribute>>();
                    logger?.LogError(ex, "Failed to log audit action");
                }
                catch
                {
                    // Ignore logger errors too
                }
            }

            base.OnActionExecuted(context);
        }

        private static string? InferActionType(string httpMethod)
        {
            return httpMethod.ToUpperInvariant() switch
            {
                "GET" => "Read",
                "POST" => "Create",
                "PUT" => "Update",
                "PATCH" => "Update",
                "DELETE" => "Delete",
                _ => httpMethod
            };
        }

        private static string? ExtractEntityIdFromRouteOrArgs(
            Microsoft.AspNetCore.Routing.RouteData routeData,
            IDictionary<string, object?>? actionArguments)
        {
            if (routeData.Values.TryGetValue("id", out var routeId) && routeId != null)
            {
                var id = routeId.ToString();
                if (!string.IsNullOrWhiteSpace(id))
                    return id;
            }

            var idParams = new[] { "entityId", "recordId", "itemId", "key", "CityId", "StateId", "CountryId", "AreaId", "CurrencyId", "ModuleId" };
            foreach (var param in idParams)
            {
                if (routeData.Values.TryGetValue(param, out var val) && val != null)
                {
                    var id = val.ToString();
                    if (!string.IsNullOrWhiteSpace(id))
                        return id;
                }
            }

            if (actionArguments != null)
            {
                foreach (var key in new[] { "id", "entityId", "recordId", "itemId", "key" })
                {
                    if (actionArguments.TryGetValue(key, out var argVal) && argVal != null)
                    {
                        var id = argVal.ToString();
                        if (!string.IsNullOrWhiteSpace(id))
                            return id;
                    }
                }
            }

            return null;
        }

        private static string? ExtractEntityIdFromResult(IActionResult? result)
        {
            if (result is not ObjectResult objectResult || objectResult.Value == null)
                return null;

            try
            {
                var value = objectResult.Value;
                var dataProp = value.GetType().GetProperty("Data", BindingFlags.Public | BindingFlags.Instance);
                if (dataProp == null)
                    return null;

                var data = dataProp.GetValue(value);
                if (data == null)
                    return null;

                // ApiResponse<int> / ApiResponse<long> / ApiResponse<string> / ApiResponse<Guid>
                if (data is int or long or short or byte or string or Guid)
                    return data.ToString();

                // ApiResponse<SomeDto> — try common *ID properties
                var type = data.GetType();
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase)
                        && !prop.Name.EndsWith("Id", StringComparison.Ordinal))
                        continue;

                    var propVal = prop.GetValue(data);
                    if (propVal == null) continue;
                    if (propVal is int or long or short or byte or string or Guid)
                        return propVal.ToString();
                }
            }
            catch
            {
                // Ignore
            }

            return null;
        }

        private static string? GetIpAddress(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                return ips[0].Trim();
            }

            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp;

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Client machine name from the caller (browser/app). Send header <c>X-Machine-Name</c> or <c>X-Client-Machine-Name</c> from the client.
        /// </summary>
        private static string? GetClientMachineName(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            const int maxLen = 200;
            string? raw = null;
            if (httpContext.Request.Headers.TryGetValue("X-Machine-Name", out var x1) && !string.IsNullOrWhiteSpace(x1))
                raw = x1.ToString();
            else if (httpContext.Request.Headers.TryGetValue("X-Client-Machine-Name", out var x2) && !string.IsNullOrWhiteSpace(x2))
                raw = x2.ToString();

            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Trim();
            return raw.Length <= maxLen ? raw : raw[..maxLen];
        }
    }
}
