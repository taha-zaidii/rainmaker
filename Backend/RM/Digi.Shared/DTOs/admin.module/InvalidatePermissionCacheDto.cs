namespace Digi.Shared.DTOs.admin.module
{
    /// <summary>
    /// DTO for cache invalidation request
    /// </summary>
    public class InvalidatePermissionCacheDto
    {
        public int? UserId { get; set; }
        public int? CompanyId { get; set; }
    }
}

