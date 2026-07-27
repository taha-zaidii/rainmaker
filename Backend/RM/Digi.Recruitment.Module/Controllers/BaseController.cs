using Digi.Shared.DTOs;
using Digi.Shared.Helper;
using Digi.Shared.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Digi.Recruitment.Module.Controllers
{
    /// <summary>
    /// Professional ERP Base Controller with Authorization
    /// All controllers inheriting from this require authentication
    /// SuperAdmin has full access, others require RECRUITMENT_ permissions
    /// </summary>
    [Authorize]
    [ModuleAuthorize("RECRUITMENT_")] // Module-wise authorization - requires any RECRUITMENT_ permission
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// Admin-like users (Administrator/Admin/Administration/SuperAdmin) can view all data.
        /// Non-admin users should be scoped to their own employee/user.
        /// </summary>
        protected bool IsAdminLike() => User.IsAdminLike();

        protected int? GetCurrentEmployeeId()
        {
            return User.GetEmployeeId();
        }

        /// <summary>
        /// If user is not admin-like, enforce employee scope (returns current EmployeeID; null if missing).
        /// If admin-like, returns the requested employeeId (can be null).
        /// </summary>
        protected int? EnforceEmployeeScope(int? requestedEmployeeId)
        {
            if (IsAdminLike())
                return requestedEmployeeId;

            return GetCurrentEmployeeId();
        }

        protected IActionResult HandleServiceResult<T>(DbOperationResult<T> result, string successMessage = null)
        {
            if (!result.IsSuccess)
            {
                if (IsNotFoundError(result.ReturnCode))
                {
                    return NotFound(ApiResponse<string>.Fail(result.Message));
                }

                return BadRequest(ApiResponse<string>.Fail(result.Message));
            }

            // Handle empty results
            if (result.Data == null)
            {
                return Ok(ApiResponse<string>.Success(null!, message: successMessage ?? "Operation completed successfully"));
            }

            // Handle collections
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var enumerable = result.Data as System.Collections.IEnumerable;
                if (enumerable == null || !enumerable.GetEnumerator().MoveNext())
                {
                    return Ok(ApiResponse<string>.Success(null!, message: "No records found"));
                }
            }

            return Ok(ApiResponse<T>.Success(
                data: result.Data,
                message: successMessage ?? "Operation completed successfully"));
        }

        protected IActionResult HandleServiceResult(DbOperationResult result, string successMessage = null)
        {
            if (!result.IsSuccess)
            {
                if (IsNotFoundError(result.ReturnCode))
                {
                    return NotFound(ApiResponse<string>.Fail(result.Message));
                }

                return BadRequest(ApiResponse<string>.Fail(result.Exception.Message));
            }

            return Ok(ApiResponse<string>.Success(null!, message: successMessage ?? "Operation completed successfully"));
        }

        private bool IsNotFoundError(int? returnCode)
        {
            var notFoundCodes = new[] { -3, 50002 };
            return returnCode.HasValue && notFoundCodes.Contains(returnCode.Value);
        }

        protected int GetCurrentUserId()
        {
            if (User?.Identity?.IsAuthenticated != true) return 0;
            var userIdClaim = User.FindFirst("UserID")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        protected int? GetCurrentCompanyId()
        {
            if (User?.Identity?.IsAuthenticated != true) return null;
            var companyIdClaim = User.FindFirst("CompanyID")?.Value
                ?? User.FindFirst("CompanyId")?.Value;
            return int.TryParse(companyIdClaim, out var companyId) ? companyId : null;
        }

        protected string? GetCurrentEmployeeCode()
        {
            if (User?.Identity?.IsAuthenticated != true) return null;
            return User.FindFirst("EmployeeCode")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.Identity?.Name;
        }
        protected string? GetCurrentCompanyName()
        {
            if (User?.Identity?.IsAuthenticated != true) return null;
            return User.FindFirst("CompanyName")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.Identity?.Name;
        }
        /// <summary>
        /// Check if current user is SuperAdmin
        /// </summary>
        protected bool IsSuperAdmin()
        {
            if (User?.Identity?.IsAuthenticated != true) return false;
            var userName = User.FindFirst("UserName")?.Value;
            return !string.IsNullOrEmpty(userName) && 
                   userName.Equals("superadmin", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if user has specific permission
        /// </summary>
        protected bool HasPermission(string permission)
        {
            if (User?.Identity?.IsAuthenticated != true) return false;
            if (IsSuperAdmin()) return true;
            return User.Claims.Any(c =>
                c.Type == "Permission" &&
                c.Value.Equals(permission, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Check if user has any of the specified permissions
        /// </summary>
        protected bool HasAnyPermission(params string[] permissions)
        {
            if (User?.Identity?.IsAuthenticated != true) return false;
            if (IsSuperAdmin()) return true;
            if (permissions == null || permissions.Length == 0) return false;
            var userPermissions = User.Claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value.ToUpper())
                .ToHashSet();
            return permissions.Any(p =>
                !string.IsNullOrEmpty(p) &&
                userPermissions.Contains(p.ToUpper()));
        }
    }
}
