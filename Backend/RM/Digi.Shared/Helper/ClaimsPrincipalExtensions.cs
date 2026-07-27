using System.Security.Claims;

namespace Digi.Shared.Helper
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true) return null;
            var raw = user.FindFirst("UserID")?.Value
                      ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("UserId")?.Value;
            return int.TryParse(raw, out var id) ? id : null;
        }
        public static string? GetEmployeeCode(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true) return null;
            return user.FindFirst("EmployeeCode")?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.Identity?.Name;
        }
        public static int? GetCompanyId(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true) return null;
            var raw = user.FindFirst("CompanyID")?.Value ?? user.FindFirst("CompanyId")?.Value;
            return int.TryParse(raw, out var id) ? id : null;
        }

        public static int? GetEmployeeId(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true) return null;
            var raw = user.FindFirst("EmployeeID")?.Value ?? user.FindFirst("EmployeeId")?.Value;
            return int.TryParse(raw, out var id) ? id : null;
        }

        public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true) return Array.Empty<string>();

            // Our system uses claim type "Role" (not ClaimTypes.Role).
            var roles = user.Claims
                .Where(c => c.Type == "Role" || c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return roles;
        }

        public static bool IsSuperAdmin(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true) return false;
            var userName = user.FindFirst("UserName")?.Value;
            return !string.IsNullOrWhiteSpace(userName) &&
                   userName.Equals("superadmin", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Admin-like means: can view all / bypass row-level scoping.
        /// Checks both role-based and permission-based admin access.
        /// </summary>
        public static bool IsAdminLike(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true) return false;
            if (user.IsSuperAdmin()) return true;

            // Check role-based admin access
            var roles = user.GetRoles();
            if (roles.Count > 0)
            {
                // Exact matches
                if (roles.Any(r =>
                        r.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                        r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                        r.Equals("Administration", StringComparison.OrdinalIgnoreCase) ||
                        r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
                    return true;

                // Fuzzy matches (e.g., "Admin User", "HRM Admin", "System Admin")
                if (roles.Any(r => r.Contains("ADMIN", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            // Check permission-based admin access
            // If user has any ADMIN_* permission, they are admin-like
            var permissions = user.Claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v));

            if (permissions.Any(p => 
                p.StartsWith("ADMIN_", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("_ADMIN", StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }
    }
}


