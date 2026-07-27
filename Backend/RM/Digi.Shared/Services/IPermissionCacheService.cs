using System.Collections.Generic;
using System.Threading.Tasks;

namespace Digi.Shared.Services
{
    /// <summary>
    /// ✅ ADVANCED ERP: Permission Cache Service with Invalidation Support
    /// Professional approach for enterprise-level permission management
    /// </summary>
    public interface IPermissionCacheService
    {
        /// <summary>
        /// Get cached permissions for user
        /// </summary>
        Task<List<string>> GetPermissionsAsync(int userId, int companyId);
        
        /// <summary>
        /// Cache permissions for user
        /// </summary>
        Task SetPermissionsAsync(int userId, int companyId, List<string> permissions);
        
        /// <summary>
        /// Invalidate cache for specific user (when permissions change)
        /// </summary>
        Task InvalidateUserCacheAsync(int userId, int companyId);
        
        /// <summary>
        /// Invalidate cache for all users in company (when role/permission changes)
        /// </summary>
        Task InvalidateCompanyCacheAsync(int companyId);
        
        /// <summary>
        /// Clear all cache (emergency/security scenarios)
        /// </summary>
        Task ClearAllCacheAsync();
    }
}

