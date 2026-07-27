using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Digi.Shared.Services
{
    /// <summary>
    /// ✅ ADVANCED ERP: Permission Cache Service with Invalidation Support
    /// Professional approach for enterprise-level permission management
    /// </summary>
    public class PermissionCacheService : IPermissionCacheService
    {
        private readonly ILogger<PermissionCacheService> _logger;
        private static readonly Dictionary<string, (List<string> Permissions, DateTime Expiry)> _permissionCache = new();
        private static readonly object _cacheLock = new object();

        public PermissionCacheService(ILogger<PermissionCacheService> logger)
        {
            _logger = logger;
        }

        public Task<List<string>> GetPermissionsAsync(int userId, int companyId)
        {
            var cacheKey = GetCacheKey(userId, companyId);
            
            lock (_cacheLock)
            {
                if (_permissionCache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
                {
                    _logger.LogDebug("✅ Cache hit for UserID: {UserId}, CompanyID: {CompanyId}", userId, companyId);
                    return Task.FromResult(cached.Permissions);
                }
            }

            _logger.LogDebug("⚠️ Cache miss for UserID: {UserId}, CompanyID: {CompanyId}", userId, companyId);
            return Task.FromResult(new List<string>());
        }

        public Task SetPermissionsAsync(int userId, int companyId, List<string> permissions)
        {
            var cacheKey = GetCacheKey(userId, companyId);
            
            lock (_cacheLock)
            {
                _permissionCache[cacheKey] = (permissions, DateTime.UtcNow.AddMinutes(5));
                _logger.LogInformation("✅ Cached {Count} permissions for UserID: {UserId}, CompanyID: {CompanyId}", 
                    permissions.Count, userId, companyId);
            }

            return Task.CompletedTask;
        }

        public Task InvalidateUserCacheAsync(int userId, int companyId)
        {
            var cacheKey = GetCacheKey(userId, companyId);
            
            lock (_cacheLock)
            {
                if (_permissionCache.Remove(cacheKey))
                {
                    _logger.LogInformation("✅ Invalidated cache for UserID: {UserId}, CompanyID: {CompanyId}", userId, companyId);
                }
                else
                {
                    _logger.LogDebug("⚠️ No cache found to invalidate for UserID: {UserId}, CompanyID: {CompanyId}", userId, companyId);
                }
            }

            return Task.CompletedTask;
        }

        public Task InvalidateCompanyCacheAsync(int companyId)
        {
            lock (_cacheLock)
            {
                var keysToRemove = _permissionCache.Keys
                    .Where(k => k.Contains($":{companyId}"))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _permissionCache.Remove(key);
                }

                _logger.LogInformation("✅ Invalidated cache for {Count} users in CompanyID: {CompanyId}", 
                    keysToRemove.Count, companyId);
            }

            return Task.CompletedTask;
        }

        public Task ClearAllCacheAsync()
        {
            lock (_cacheLock)
            {
                var count = _permissionCache.Count;
                _permissionCache.Clear();
                _logger.LogWarning("⚠️ Cleared all permission cache ({Count} entries)", count);
            }

            return Task.CompletedTask;
        }

        private string GetCacheKey(int userId, int companyId)
        {
            return $"permissions_{userId}_{companyId}";
        }
    }
}

