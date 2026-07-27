using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Digi.Admin.Module.Helper
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using System.Linq;
    using System.Security.Claims;

    public class PermissionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _requiredPermission;

        public PermissionAuthorizeAttribute(string requiredPermission)
        {
            _requiredPermission = requiredPermission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Check if user is authenticated
            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // SuperAdmin Bypass
            if (user.HasClaim(c => c.Type == ClaimTypes.Email && c.Value == "superadmin@gmail.com"))
            {
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

    //public class PermissionAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    //{
    //    private readonly string _requiredPermission;

    //    public PermissionAuthorizeAttribute(string permission)
    //    {
    //        _requiredPermission = permission;
    //    }

    //    public void OnAuthorization(AuthorizationFilterContext context)
    //    {
    //        var user = context.HttpContext.User;
    //        if (!user.Identity.IsAuthenticated)
    //        {
    //            context.Result = new UnauthorizedResult();
    //            return;
    //        }

    //        var hasPermission = user.Claims.Any(c =>
    //            c.Type == "Permission" && c.Value == _requiredPermission);

    //        if (!hasPermission)
    //        {
    //            context.Result = new ForbidResult();
    //        }
    //    }
    //}

}
