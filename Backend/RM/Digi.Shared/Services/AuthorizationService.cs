using System.Security.Claims;

namespace Digi.Shared.Services
{
    /// <summary>
    /// Professional ERP Authorization Service
    /// Centralized authorization logic with SuperAdmin bypass
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Check if user is SuperAdmin
        /// </summary>
        bool IsSuperAdmin(ClaimsPrincipal user);

        /// <summary>
        /// Check if user has specific permission
        /// </summary>
        bool HasPermission(ClaimsPrincipal user, string permission);

        /// <summary>
        /// Check if user has any permission for a module
        /// </summary>
        bool HasModuleAccess(ClaimsPrincipal user, string modulePrefix);

        /// <summary>
        /// Check if user has any of the specified permissions
        /// </summary>
        bool HasAnyPermission(ClaimsPrincipal user, params string[] permissions);

        /// <summary>
        /// Check if user has all specified permissions
        /// </summary>
        bool HasAllPermissions(ClaimsPrincipal user, params string[] permissions);
    }

    public class AuthorizationService : IAuthorizationService
    {
        public bool IsSuperAdmin(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var userName = user.FindFirst("UserName")?.Value;
            return !string.IsNullOrEmpty(userName) && 
                   userName.Equals("superadmin", StringComparison.OrdinalIgnoreCase);
        }

        public bool HasPermission(ClaimsPrincipal user, string permission)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            // SuperAdmin bypass
            if (IsSuperAdmin(user))
                return true;

            // Check permission claim
            return user.Claims.Any(c =>
                c.Type == "Permission" && 
                c.Value.Equals(permission, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasModuleAccess(ClaimsPrincipal user, string modulePrefix)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            // SuperAdmin bypass
            if (IsSuperAdmin(user))
                return true;

            if (string.IsNullOrEmpty(modulePrefix))
                return false;

            // Check if user has any permission starting with module prefix
            return user.Claims.Any(c =>
                c.Type == "Permission" &&
                !string.IsNullOrEmpty(c.Value) &&
                c.Value.StartsWith(modulePrefix.ToUpper(), StringComparison.OrdinalIgnoreCase));
        }

        public bool HasAnyPermission(ClaimsPrincipal user, params string[] permissions)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            // SuperAdmin bypass
            if (IsSuperAdmin(user))
                return true;

            if (permissions == null || permissions.Length == 0)
                return false;

            var userPermissions = user.Claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value.ToUpper())
                .ToHashSet();

            return permissions.Any(p => 
                !string.IsNullOrEmpty(p) && 
                userPermissions.Contains(p.ToUpper()));
        }

        public bool HasAllPermissions(ClaimsPrincipal user, params string[] permissions)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            // SuperAdmin bypass
            if (IsSuperAdmin(user))
                return true;

            if (permissions == null || permissions.Length == 0)
                return false;

            var userPermissions = user.Claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value.ToUpper())
                .ToHashSet();

            return permissions.All(p =>
                !string.IsNullOrEmpty(p) &&
                userPermissions.Contains(p.ToUpper()));
        }
    }
}

