using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Security.Claims;

namespace Digi.Shared.Attributes
{
    /// <summary>
    /// Professional ERP Permission-based Authorization Attribute
    /// Checks if user has required permission, SuperAdmin bypass included
    /// </summary>
    public class PermissionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _requiredPermission;

        public PermissionAuthorizeAttribute(string requiredPermission)
        {
            _requiredPermission = requiredPermission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if [AllowAnonymous] attribute is present - if yes, skip authorization
            // Method 1: Check endpoint metadata
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                return; // Allow anonymous access
            }

            // Method 2: Check action descriptor metadata
            if (context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowAnonymousAttribute))
            {
                return; // Allow anonymous access
            }

            // Method 3: Check controller action descriptor method info
            if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
            {
                if (actionDescriptor.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any() ||
                    actionDescriptor.ControllerTypeInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any())
                {
                    return; // Allow anonymous access
                }
            }

            // Method 4: Check filters
            if (context.Filters.Any(f => f is AllowAnonymousAttribute))
            {
                return; // Allow anonymous access
            }

            var user = context.HttpContext.User;

            // Check if user is authenticated
            if (!user.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    isSuccess = false,
                    message = "Authentication required"
                });
                return;
            }

            // SuperAdmin Bypass - Professional ERP approach
            var userName = user.FindFirst("UserName")?.Value;
            if (!string.IsNullOrEmpty(userName) && userName.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
            {
                // SuperAdmin has access to everything
                return;
            }

            // Check if required permission exists
            var hasPermission = user.Claims.Any(c =>
                c.Type == "Permission" && c.Value == _requiredPermission);

            if (!hasPermission)
            {
                context.Result = new ForbidResult(); // 403 Forbidden
            }
        }
    }
}

