using Digi.Shared.DTOs.admin.module;

namespace Digi.Shared.DTOs.admin.module
{
    public interface ISmtpRepository
    {
        Task<IEnumerable<SmtpResponseDto>> ExecuteAsync(SmtpRequestDto dto);
        Task<IEnumerable<SmtpResponseDto>> GetAllSuperAdminAsync();
        Task<SmtpResponseDto?> GetSmtpByCompanyIdAsync(int? companyId);
    }
}
