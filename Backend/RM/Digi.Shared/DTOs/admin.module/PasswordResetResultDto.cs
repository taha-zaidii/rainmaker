using Digi.Shared.Services;

namespace Digi.Shared.DTOs.admin.module
{
    public class PasswordResetResultDto
    {
        public bool RequiresReLogin { get; set; } = true;
        public string Reason { get; set; } = SessionRevocationConstants.ReasonPasswordChanged;
        public string Message { get; set; } = string.Empty;
    }
}
