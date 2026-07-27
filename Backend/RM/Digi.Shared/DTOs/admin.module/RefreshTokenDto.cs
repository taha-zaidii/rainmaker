namespace Digi.Shared.DTOs.admin.module
{
    public class RefreshTokenDto
    {
        public string? RefreshToken { get; set; }
    }
    
    // Public response DTO (client ko access token + permissions)
    public class TokenResponseDto
    {
        public string Token { get; set; }
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Same value as HttpOnly cookie — native apps (especially older iOS) often do not persist cross-origin cookies; optional duplicate for clients that read the body only.
        /// </summary>
        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiry { get; set; }

        /// <summary>
        /// Profile images from the same source as get-user-profile-images, included on login/refresh so clients do not need an immediate second authenticated call.
        /// </summary>
        public UserProfileImagesDto? ProfileImages { get; set; }
        
        // ✅ Permissions for frontend - fetched from database during login
        public List<string> Permissions { get; set; } = new List<string>();
        
        // Optional: User info for frontend
        //public int? UserId { get; set; }
        //public string UserName { get; set; }
        //public int? CompanyId { get; set; }
        //public string Email { get; set; }
        //public List<string> Roles { get; set; } = new List<string>();
    }

    // Internal/Service response DTO (controller cookie set karne ke liye)
    public class RefreshTokenRotationResultDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
        public DateTime? TokenExpiry { get; set; }
        
        // ✅ Permissions for frontend - fetched from database
        public List<string> Permissions { get; set; } = new List<string>();

        public UserProfileImagesDto? ProfileImages { get; set; }
        
        // Optional: User info for frontend
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public int? CompanyId { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
    
    public class RefreshTokenValidationResultDto
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public int? UserID { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int? CompanyID { get; set; }
        // Optional: SP can populate for reuse/forensics
        public bool? IsRevoked { get; set; }
        public DateTime? RevokedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}

