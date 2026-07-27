namespace Digi.Shared.DTOs.admin.module
{
    /// <summary>
    /// DTO for user profile images (EmployeeThumbnail and CompanyLogo)
    /// Used when these URLs are fetched separately from JWT token
    /// </summary>
    public class UserProfileImagesDto
    {
        public string? EmployeeThumbnail { get; set; }
        public string? CompanyLogo { get; set; }
    }
}

