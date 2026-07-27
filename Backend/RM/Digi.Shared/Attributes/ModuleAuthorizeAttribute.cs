using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Security.Claims;
using Digi.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Digi.Shared.Attributes
{
    /// <summary>
    /// Module-wise Authorization Attribute
    /// Checks if user has any permission for the module
    /// SuperAdmin bypass included
    /// </summary>
    public class ModuleAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _modulePrefix;

        // ERP menu/module name aliases (because DB module names don't always match code prefixes)
        // Example: GEN_ module is shown as "Master Data" in UI/DB.
        private static readonly Dictionary<string, string[]> _moduleAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            // codeKey => accepted module names/keys in Modules claim
            ["GEN"] = new[] { "GEN", "MASTER DATA", "MASTERDATA", "MASTER_DATA", "GENERAL" },
            ["ADMIN"] = new[] { "ADMIN", "ADMINISTRATION" },
            ["HRM"] = new[] { "HRM" },
            ["PROC"] = new[] { "PROC", "PROCUREMENT" },
            ["SALES"] = new[] { "SALES" },
            ["FINANCE"] = new[] { "FINANCE", "ACCOUNTING" },
            ["CRM"] = new[] { "CRM" },
            ["VMS"] = new[] { "VMS" },
            ["EMS"] = new[] { "EMS" },
            ["QUALITY"] = new[] { "QUALITY" },
            ["ASSET"] = new[] { "ASSET", "INVENTORY" },
            ["MANUFACTURING"] = new[] { "MANUFACTURING" },
            ["NOTIFICATION"] = new[] { "NOTIFICATION" },
            ["LEARNING CENTER"] = new[] { "LEARNING CENTER" },
            ["PMP"] = new[] { "PMP", "PROJECT MANAGEMENT", "PROJECT_MANAGEMENT" },
        };

        public ModuleAuthorizeAttribute(string modulePrefix)
        {
            _modulePrefix = modulePrefix?.ToUpper() ?? string.Empty;
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

            // SuperAdmin Bypass - Check FIRST before permission check
            var userName = user.FindFirst("UserName")?.Value;
            if (!string.IsNullOrEmpty(userName) && userName.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
            {
                return; // SuperAdmin has full access
            }

            // ✅ ERP-level module access: prefer module list claim over string-prefix permissions.
            // We derive module key from prefix (e.g., "HRM_" -> "HRM") and check "Modules" claim.
            var moduleKey = _modulePrefix?.Trim().TrimEnd('_')?.ToUpperInvariant();
            if (!string.IsNullOrEmpty(moduleKey))
            {
                var modulesClaim = user.FindFirst("Modules");
                if (modulesClaim != null && !string.IsNullOrWhiteSpace(modulesClaim.Value))
                {
                    try
                    {
                        // Modules claim is expected to be JSON array, but we also tolerate base64(JSON).
                        var raw = modulesClaim.Value;
                        List<string>? modules = null;

                        try
                        {
                            modules = JsonSerializer.Deserialize<List<string>>(raw);
                        }
                        catch
                        {
                            // Try base64 decode then JSON
                            var bytes = Convert.FromBase64String(raw);
                            var json = System.Text.Encoding.UTF8.GetString(bytes);
                            modules = JsonSerializer.Deserialize<List<string>>(json);
                        }

                        if (modules != null)
                        {
                            var normalized = modules
                                .Where(m => !string.IsNullOrWhiteSpace(m))
                                .Select(m => m.Trim().ToUpperInvariant())
                                .ToHashSet();

                            // Allow if module exists (exact) OR module name begins with the key (e.g., "HRM MODULE")
                            // Also allow ERP aliases (e.g., GEN -> MASTER DATA)
                            var allowedNames = _moduleAliases.TryGetValue(moduleKey, out var aliases)
                                ? aliases.Select(a => a.Trim().ToUpperInvariant()).ToList()
                                : new List<string> { moduleKey };

                            if (normalized.Overlaps(allowedNames) ||
                                normalized.Any(m => allowedNames.Any(a => m.StartsWith(a))))
                            {
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<ModuleAuthorizeAttribute>>();
                        logger?.LogError(ex, "Error parsing Modules claim for module authorization");
                    }
                }
            }

            // Get all user permissions for debugging
            var allPermissions = user.Claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .ToList();

            // Also check compressed Permissions claim (in case middleware didn't decompress)
            // Check compressed claim even if individual permissions exist (middleware might have failed)
            var compressedPermissionsClaim = user.FindFirst("Permissions");
            if (compressedPermissionsClaim != null)
            {
                // Try to decompress manually if middleware didn't work
                try
                {
                    var tokenOptimizationService = context.HttpContext.RequestServices.GetService<ITokenOptimizationService>();
                    if (tokenOptimizationService != null)
                    {
                        var decompressed = tokenOptimizationService.DecompressPermissions(compressedPermissionsClaim);
                        allPermissions.AddRange(decompressed);
                    }
                    else
                    {
                        // Fallback: Manual decompression with base64 detection
                        try
                        {
                            string jsonString = compressedPermissionsClaim.Value;
                            
                            // Try base64 decode first
                            try
                            {
                                var bytes = Convert.FromBase64String(compressedPermissionsClaim.Value);
                                jsonString = System.Text.Encoding.UTF8.GetString(bytes);
                            }
                            catch
                            {
                                // Not base64, use as-is
                                jsonString = compressedPermissionsClaim.Value;
                            }

                            var decompressed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonString);
                            if (decompressed != null)
                            {
                                allPermissions.AddRange(decompressed);
                            }
                        }
                        catch (Exception ex)
                        {
                            var logger = context.HttpContext.RequestServices.GetService<ILogger<ModuleAuthorizeAttribute>>();
                            logger?.LogError(ex, "Error manually decompressing permissions (fallback)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<ModuleAuthorizeAttribute>>();
                    logger?.LogError(ex, "Error manually decompressing permissions in ModuleAuthorizeAttribute");
                }
            }

            // Check if user has any permission for this module
            var hasModulePermission = allPermissions.Any(p =>
                !string.IsNullOrEmpty(p) &&
                p.StartsWith(_modulePrefix, StringComparison.OrdinalIgnoreCase));

            if (!hasModulePermission)
            {
                // Log for debugging with detailed information
                var logger = context.HttpContext.RequestServices.GetService<ILogger<ModuleAuthorizeAttribute>>();
                logger?.LogWarning(
                    "❌ User {UserName} (ID: {UserId}) denied access to module {ModulePrefix}. User has {PermissionCount} permissions. First 20 permissions: {Permissions}",
                    userName,
                    user.FindFirst("UserID")?.Value ?? "Unknown",
                    _modulePrefix,
                    allPermissions.Count,
                    string.Join(", ", allPermissions.Take(20)));

                // Log all permissions for debugging (if needed)
                if (allPermissions.Count > 0)
                {
                    logger?.LogDebug("All user permissions: {AllPermissions}", string.Join(", ", allPermissions));
                }

                // Return detailed error message for debugging
                context.Result = new ForbidResult(); // 403 Forbidden
                
                // Alternative: Return JSON response with details (uncomment if needed for debugging)
                // context.Result = new JsonResult(new
                // {
                //     isSuccess = false,
                //     message = $"Access denied. Required module prefix: {_modulePrefix}. User has {allPermissions.Count} permissions.",
                //     requiredPrefix = _modulePrefix,
                //     userPermissions = allPermissions.Take(10).ToList()
                // })
                // {
                //     StatusCode = 403
                // };
            }
        }
    }
}

