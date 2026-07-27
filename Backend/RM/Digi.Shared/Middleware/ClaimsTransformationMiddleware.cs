using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Digi.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Digi.Shared.SharedLibrary.Interfaces;
using System.Data;
using System.Linq;
using Digi.Shared.DTOs.admin.module;

namespace Digi.Shared.Middleware
{
    /// <summary>
    /// Advanced ERP Middleware: Fetches permissions from database instead of token
    /// Dramatically reduces token size from 16KB+ to <2KB
    /// </summary>
    public class ClaimsTransformationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ClaimsTransformationMiddleware> _logger;

        public ClaimsTransformationMiddleware(
            RequestDelegate next, 
            ILogger<ClaimsTransformationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Skip middleware for login/authentication endpoints
                var path = context.Request.Path.Value?.ToLower() ?? "";
                if (path.Contains("/api/auth/login") || path.Contains("/api/auth/refresh") || path.Contains("/swagger"))
                {
                    await _next(context);
                    return;
                }

            if (context.User?.Identity?.IsAuthenticated == true && context.User.Identity is ClaimsIdentity identity)
            {
                // Create a new mutable identity with existing claims
                // JWT authentication creates read-only identity, so we need to create a new one
                var mutableIdentity = new ClaimsIdentity(
                    identity.AuthenticationType,
                    identity.NameClaimType,
                    identity.RoleClaimType);
                
                // Copy all existing claims to mutable identity
                foreach (var claim in identity.Claims)
                {
                    mutableIdentity.AddClaim(claim);
                }
                
                // Replace the identity in the principal
                var principal = new System.Security.Claims.ClaimsPrincipal(mutableIdentity);
                context.User = principal;
                identity = mutableIdentity;

                // ✅ ADVANCED ERP APPROACH: Check if permissions should be fetched from database
                var fetchFromDb = identity.FindFirst("FetchPermissionsFromDB");
                var hasPermissionsClaim = identity.FindFirst("Permissions") != null;
                var hasIndividualPermissions = identity.Claims.Any(c => c.Type == "Permission");

                // If FetchPermissionsFromDB flag exists OR no permissions in token, fetch from database
                // This handles both new tokens (with flag) and edge cases (no flag but no permissions)
                if ((fetchFromDb != null || (!hasPermissionsClaim && !hasIndividualPermissions)) && !hasIndividualPermissions)
                {
                    try
                    {
                        var userId = identity.FindFirst("UserID")?.Value;
                        var companyId = identity.FindFirst("CompanyID")?.Value;

                        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(companyId))
                        {
                            var permissions = await FetchPermissionsFromDatabase(context, userId, companyId);
                            
                            // Add individual permission claims
                            var addedCount = 0;
                            foreach (var permission in permissions)
                            {
                                if (!string.IsNullOrWhiteSpace(permission) && !identity.HasClaim("Permission", permission))
                                {
                                    identity.AddClaim(new Claim("Permission", permission));
                                    addedCount++;
                                }
                            }

                            _logger.LogInformation("✅ Fetched {Count} permissions from database for user {UserName} (UserID: {UserId}). Added {AddedCount} new claims.", 
                                permissions.Count, 
                                identity.FindFirst("UserName")?.Value ?? "Unknown",
                                userId,
                                addedCount);
                            
                            // ✅ CRITICAL: Verify permissions were added (for debugging)
                            var totalPermissions = identity.Claims.Count(c => c.Type == "Permission");
                            _logger.LogDebug("📊 Total Permission claims after fetch: {Count}", totalPermissions);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ UserID or CompanyID missing in token claims. UserID: {UserId}, CompanyID: {CompanyId}", 
                                userId ?? "null", companyId ?? "null");
                        }
                    }
                    catch (Exception ex)
                    {
                        // ✅ CRITICAL: Don't fail the request if permissions fetch fails
                        // Log error but allow request to continue (user might have no permissions)
                        _logger.LogError(ex, "❌ Error fetching permissions from database - continuing without permissions");
                        // Request continues - user will have no permissions but won't get 401
                    }
                }
                // Backward compatibility: Handle compressed permissions from token (old tokens)
                else
                {
                var tokenOptimizationService = context.RequestServices.GetService<ITokenOptimizationService>();
                    var permissionsClaim = identity.FindFirst("Permissions");

                if (permissionsClaim != null && tokenOptimizationService != null)
                {
                    try
                    {
                        // Decompress permissions
                        var permissions = tokenOptimizationService.DecompressPermissions(permissionsClaim);
                        
                        // Add individual permission claims for backward compatibility
                        foreach (var permission in permissions)
                        {
                            if (!string.IsNullOrWhiteSpace(permission) && !identity.HasClaim("Permission", permission))
                            {
                                identity.AddClaim(new Claim("Permission", permission));
                            }
                        }
                        
                        _logger.LogInformation("✅ Decompressed {Count} permissions from token for user {UserName}", 
                            permissions.Count, 
                            identity.FindFirst("UserName")?.Value ?? "Unknown");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error decompressing permissions from token");
                    }
                }
                else if (permissionsClaim != null && tokenOptimizationService == null)
                {
                    // Fallback: Manual decompression if service not available
                    try
                    {
                        string jsonString = permissionsClaim.Value;
                        
                        // Try base64 decode first
                        try
                        {
                            var bytes = Convert.FromBase64String(permissionsClaim.Value);
                            jsonString = System.Text.Encoding.UTF8.GetString(bytes);
                            _logger.LogDebug("Decoded base64 permissions (fallback)");
                        }
                        catch
                        {
                            // Not base64, use as-is
                            jsonString = permissionsClaim.Value;
                        }

                        var permissions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonString);
                        if (permissions != null)
                        {
                            foreach (var permission in permissions)
                            {
                                if (!string.IsNullOrWhiteSpace(permission) && !identity.HasClaim("Permission", permission))
                                {
                                    identity.AddClaim(new Claim("Permission", permission));
                                }
                            }
                            _logger.LogInformation("✅ Decompressed {Count} permissions (fallback) for user {UserName}", 
                                permissions.Count,
                                identity.FindFirst("UserName")?.Value ?? "Unknown");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error decompressing permissions from token (fallback): {Error}", ex.Message);
                    }
                }
                }

                // Handle compressed roles
                var rolesClaim = identity.FindFirst("Roles");
                if (rolesClaim != null)
                {
                    try
                    {
                        var roles = System.Text.Json.JsonSerializer.Deserialize<List<string>>(rolesClaim.Value);
                        if (roles != null)
                        {
                            foreach (var role in roles)
                            {
                                if (!identity.HasClaim("Role", role))
                                {
                                    identity.AddClaim(new Claim("Role", role));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error decompressing roles from token");
                    }
                }
                }
            }
            catch (Exception ex)
            {
                // ✅ CRITICAL: Don't fail the request if middleware has any error
                // Log error but allow request to continue
                _logger.LogError(ex, "❌ Error in ClaimsTransformationMiddleware - continuing without transformation");
            }

            await _next(context);
        }

        /// <summary>
        /// ✅ ADVANCED ERP APPROACH: Fetch permissions from database with caching
        /// </summary>
        private async Task<List<string>> FetchPermissionsFromDatabase(HttpContext context, string userId, string companyId)
        {
            // ✅ Use PermissionCacheService for centralized cache management
            var cacheService = context.RequestServices.GetService<IPermissionCacheService>();
            if (cacheService != null)
            {
                var cachedPermissions = await cacheService.GetPermissionsAsync(int.Parse(userId), int.Parse(companyId));
                if (cachedPermissions.Any())
                {
                    _logger.LogDebug("✅ Using cached permissions for user {UserId}", userId);
                    return cachedPermissions;
                }
            }

            // Fetch from database
            var dapperService = context.RequestServices.GetService<IDapperService>();
            if (dapperService == null)
            {
                _logger.LogWarning("⚠️ IDapperService not available, returning empty permissions");
                return new List<string>();
            }

            try
            {
                // ✅ Use PermissionDto instead of dynamic for proper type safety
                var permissions = await dapperService.QueryAsync<PermissionDto>(
                    "sp_Adm_GetUserPermissions_v2",
                    new { UserId = int.Parse(userId), CompanyId = int.Parse(companyId) },
                    CommandType.StoredProcedure);

                var permissionNames = permissions
                    .Where(p => !string.IsNullOrWhiteSpace(p?.PermissionName))
                    .Select(p => p.PermissionName!.Trim())
                    .Distinct()
                    .ToList();
                
                _logger.LogDebug("📊 Fetched {Count} unique permissions from database for UserID: {UserId}, CompanyID: {CompanyId}", 
                    permissionNames.Count, userId, companyId);

                // ✅ Cache using PermissionCacheService
                if (cacheService != null)
                {
                    await cacheService.SetPermissionsAsync(int.Parse(userId), int.Parse(companyId), permissionNames);
                }

                return permissionNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching permissions from database for UserID: {UserId}, CompanyID: {CompanyId}", userId, companyId);
                return new List<string>();
            }
        }
    }
}

