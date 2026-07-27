namespace Digi.Shared.DTOs.admin.module
{
    public class UserRoleNavigationPermissionDto
    {
        public int NavId { get; set; }
        public int? ParentID { get; set; }
        public string? DisplayName { get; set; }
        public string? DisplayOrder { get; set; }
        public string? RouteName { get; set; }
        public string? IconURL { get; set; }
        public int ModuleId { get; set; }
        public bool IsActive { get; set; }

        public List<UserRoleNavigationPermissionDto> Children { get; set; } = new();
    }
}
