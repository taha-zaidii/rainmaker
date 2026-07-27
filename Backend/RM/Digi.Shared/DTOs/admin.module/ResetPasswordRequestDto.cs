namespace Digi.Shared.DTOs.admin.module
{
    public class ResetPasswordRequestDto
    {
        public string? Email { get; set; }
        public string UserName { get; set; }
        public string? Token { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }
    public class ResetPasswordInternalRequestDto
    {
        public string? Email { get; set; }
        public string UserName { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
