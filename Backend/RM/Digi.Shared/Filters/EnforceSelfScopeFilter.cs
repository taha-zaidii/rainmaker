using Digi.Shared.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Digi.Shared.Filters
{
    /// <summary>
    /// Advanced ERP row-level scoping:
    /// - Admin-like users: can access any scope
    /// - Non-admin users: employeeId/userId/companyId/approverId are forced to current user's values
    ///
    /// This prevents "employee A" from requesting "employee B" data by changing query params/body.
    /// </summary>
    public sealed class EnforceSelfScopeFilter : IAsyncActionFilter
    {
        private readonly ILogger<EnforceSelfScopeFilter> _logger;

        public EnforceSelfScopeFilter(ILogger<EnforceSelfScopeFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            // Opt-out support
            if (IsSkipScope(context))
            {
                await next();
                return;
            }

            // Admin-like bypass (includes superadmin)
            if (user.IsAdminLike())
            {
                await next();
                return;
            }

            var currentEmployeeId = user.GetEmployeeId();
            var currentUserId = user.GetUserId();
            var currentCompanyId = user.GetCompanyId();

            // If we can't scope (missing claims), let endpoint handle it (will often return 401/400).
            if (!currentEmployeeId.HasValue && !currentUserId.HasValue && !currentCompanyId.HasValue)
            {
                await next();
                return;
            }

            // 1) Force primitive action arguments
            ForceArg(context, new[] { "employeeId", "employeeID", "EmployeeId", "EmployeeID" }, currentEmployeeId);
            ForceArg(context, new[] { "approverId", "approverID", "ApproverId", "ApproverID" }, currentEmployeeId);
            ForceArg(context, new[] { "userId", "userID", "UserId", "UserID" }, currentUserId);
            ForceArg(context, new[] { "companyId", "companyID", "CompanyId", "CompanyID" }, currentCompanyId);

            // 2) Force DTO properties (best-effort) for common names
            foreach (var arg in context.ActionArguments.Values)
            {
                if (arg == null) continue;

                TryForceProperty(arg, new[] { "EmployeeId", "EmployeeID" }, currentEmployeeId);
                TryForceProperty(arg, new[] { "ApproverId", "ApproverID" }, currentEmployeeId);
                TryForceProperty(arg, new[] { "UserId", "UserID" }, currentUserId);
                TryForceProperty(arg, new[] { "CompanyId", "CompanyID" }, currentCompanyId);
            }

            await next();
        }

        private static void ForceArg(ActionExecutingContext context, IEnumerable<string> keys, int? value)
        {
            if (!value.HasValue) return;

            foreach (var key in keys)
            {
                if (!context.ActionArguments.ContainsKey(key)) continue;

                var current = context.ActionArguments[key];
                if (current is int)
                {
                    context.ActionArguments[key] = value.Value;
                }
                else if (current is int?)
                {
                    context.ActionArguments[key] = value;
                }
            }
        }

        private static void TryForceProperty(object target, IEnumerable<string> propertyNames, int? value)
        {
            if (target == null || !value.HasValue) return;

            var type = target.GetType();

            foreach (var name in propertyNames)
            {
                var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null || !prop.CanWrite) continue;

                if (prop.PropertyType == typeof(int))
                {
                    prop.SetValue(target, value.Value);
                    return;
                }

                if (prop.PropertyType == typeof(int?))
                {
                    prop.SetValue(target, value);
                    return;
                }
            }
        }

        private static bool IsSkipScope(ActionExecutingContext context)
        {
            // Endpoint metadata / attributes
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<SkipSelfScopeAttribute>() != null)
                return true;

            if (context.ActionDescriptor is ControllerActionDescriptor cad)
            {
                if (cad.MethodInfo.GetCustomAttributes(typeof(SkipSelfScopeAttribute), true).Any())
                    return true;

                if (cad.ControllerTypeInfo.GetCustomAttributes(typeof(SkipSelfScopeAttribute), true).Any())
                    return true;
            }

            return false;
        }
    }
}


