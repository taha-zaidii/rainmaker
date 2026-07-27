using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Digi.Shared.Services
{
    /// <summary>
    /// Professional token optimization service for ERP systems
    /// Reduces token size by compressing claims and using efficient encoding
    /// </summary>
    public interface ITokenOptimizationService
    {
        /// <summary>
        /// Optimize claims list to reduce token size
        /// </summary>
        List<Claim> OptimizeClaims(List<Claim> claims, int maxTokenSizeBytes = 4096);
        
        /// <summary>
        /// Compress permissions into a single claim using JSON array
        /// </summary>
        Claim CompressPermissions(List<string> permissions);
        
        /// <summary>
        /// Decompress permissions from claim
        /// </summary>
        List<string> DecompressPermissions(Claim claim);
        
        /// <summary>
        /// Estimate token size in bytes
        /// </summary>
        int EstimateTokenSize(List<Claim> claims);
        
        /// <summary>
        /// Remove unnecessary claims that can be fetched from database
        /// </summary>
        List<Claim> RemoveNonEssentialClaims(List<Claim> claims);
    }

    public class TokenOptimizationService : ITokenOptimizationService
    {
        private readonly ILogger<TokenOptimizationService> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        public TokenOptimizationService(ILogger<TokenOptimizationService> logger)
        {
            _logger = logger;
        }

        public List<Claim> OptimizeClaims(List<Claim> claims, int maxTokenSizeBytes = 4096)
        {
            try
            {
                var optimizedClaims = new List<Claim>();
                var permissions = new List<string>();
                var roles = new List<string>();
                var roleIds = new List<string>();

                // Separate essential and non-essential claims
                foreach (var claim in claims)
                {
                    switch (claim.Type)
                    {
                        case "Permission":
                            // ✅ ADVANCED ERP APPROACH: Remove permissions from token completely
                            // Permissions will be fetched from database in ClaimsTransformationMiddleware
                            // This dramatically reduces token size (from 16KB+ to <2KB)
                            permissions.Add(claim.Value);
                            _logger.LogDebug("Removed Permission claim from token: {Permission} - will be fetched from DB", claim.Value);
                            break;
                        case "Role":
                            roles.Add(claim.Value);
                            break;
                        case "RoleID":
                            roleIds.Add(claim.Value);
                            break;
                        case "EmployeeThumbnail":
                        case "CompanyLogo":
                            // Remove large URLs - fetch from API when needed
                            _logger.LogDebug("Removed non-essential claim: {ClaimType}", claim.Type);
                            break;
                        default:
                            optimizedClaims.Add(claim);
                            break;
                    }
                }

                // ✅ ADVANCED ERP APPROACH: Do NOT add permissions to token
                // Always add flag to indicate permissions should be fetched from database
                // This ensures middleware knows to fetch permissions even if user has no permissions currently
                optimizedClaims.Add(new Claim("FetchPermissionsFromDB", "true"));
                if (permissions.Any())
                {
                    _logger.LogInformation("✅ Removed {Count} permissions from token - will be fetched from database", permissions.Count);
                }
                else
                {
                    _logger.LogInformation("✅ Token configured to fetch permissions from database (user has no permissions currently)");
                }

                // Compress roles if multiple
                if (roles.Count > 1)
                {
                    var rolesJson = JsonSerializer.Serialize(roles, _jsonOptions);
                    optimizedClaims.Add(new Claim("Roles", rolesJson));
                }
                else if (roles.Count == 1)
                {
                    optimizedClaims.Add(new Claim("Role", roles[0]));
                }

                // Compress role IDs if multiple
                if (roleIds.Count > 1)
                {
                    var roleIdsJson = JsonSerializer.Serialize(roleIds, _jsonOptions);
                    optimizedClaims.Add(new Claim("RoleIDs", roleIdsJson));
                }
                else if (roleIds.Count == 1)
                {
                    optimizedClaims.Add(new Claim("RoleID", roleIds[0]));
                }

                // Estimate size and log warning if still too large
                var estimatedSize = EstimateTokenSize(optimizedClaims);
                if (estimatedSize > maxTokenSizeBytes)
                {
                    _logger.LogWarning("Token size ({Size} bytes) exceeds recommended limit ({MaxSize} bytes)", 
                        estimatedSize, maxTokenSizeBytes);
                }
                else
                {
                    _logger.LogInformation("Token optimized: {OriginalCount} -> {OptimizedCount} claims, Size: ~{Size} bytes", 
                        claims.Count, optimizedClaims.Count, estimatedSize);
                }

                return optimizedClaims;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing claims");
                return claims; // Return original if optimization fails
            }
        }

        public Claim CompressPermissions(List<string> permissions)
        {
            if (permissions == null || !permissions.Any())
            {
                return new Claim("Permissions", "[]");
            }

            // Use compact JSON format
            var json = JsonSerializer.Serialize(permissions, _jsonOptions);
            
            // If still too large, use base64 encoding (rare case)
            if (Encoding.UTF8.GetByteCount(json) > 2000)
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                var base64 = Convert.ToBase64String(bytes);
                return new Claim("Permissions", base64, "base64");
            }

            return new Claim("Permissions", json);
        }

        public List<string> DecompressPermissions(Claim claim)
        {
            if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
            {
                return new List<string>();
            }

            try
            {
                string jsonString = claim.Value;

                // Check if base64 encoded (by ValueType or by trying to decode)
                if (claim.ValueType == "base64" || IsBase64String(claim.Value))
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(claim.Value);
                        jsonString = Encoding.UTF8.GetString(bytes);
                        _logger.LogDebug("Decoded base64 permissions claim");
                    }
                    catch
                    {
                        // Not base64, use as-is
                        jsonString = claim.Value;
                    }
                }

                // Try to deserialize JSON
                var permissions = JsonSerializer.Deserialize<List<string>>(jsonString);
                return permissions ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decompressing permissions: {Error}", ex.Message);
                return new List<string>();
            }
        }

        /// <summary>
        /// Check if string is base64 encoded
        /// </summary>
        private bool IsBase64String(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Base64 strings are typically longer and contain only base64 characters
            if (value.Length < 10)
                return false;

            try
            {
                // Try to decode - if successful and result is valid UTF-8, it's likely base64
                var bytes = Convert.FromBase64String(value);
                var decoded = Encoding.UTF8.GetString(bytes);
                // Check if decoded string looks like JSON (starts with [ or {)
                return decoded.TrimStart().StartsWith("[") || decoded.TrimStart().StartsWith("{");
            }
            catch
            {
                return false;
            }
        }

        public int EstimateTokenSize(List<Claim> claims)
        {
            // Rough estimation: each claim adds ~50-200 bytes depending on value
            // Base64 encoding adds ~33% overhead
            var baseSize = 200; // JWT header + signature overhead
            var claimsSize = claims.Sum(c => Encoding.UTF8.GetByteCount(c.Type) + Encoding.UTF8.GetByteCount(c.Value) + 50);
            return (int)((baseSize + claimsSize) * 1.33); // Base64 overhead
        }

        public List<Claim> RemoveNonEssentialClaims(List<Claim> claims)
        {
            var essentialClaimTypes = new HashSet<string>
            {
                "UserID",
                "UserName",
                "Email",
                "CompanyID",
                "EmployeeID",
                "EmployeeCode",
                "CompanyName",
                "DepartmentID",
                "GeoFenceID",
                "Permissions",
                "Roles",
                "Role",
                "RoleID",
                "RoleIDs",
                "sub",
                "iss",
                "aud",
                "exp",
                "iat",
                "nbf"
            };

            return claims.Where(c => essentialClaimTypes.Contains(c.Type)).ToList();
        }
    }
}

