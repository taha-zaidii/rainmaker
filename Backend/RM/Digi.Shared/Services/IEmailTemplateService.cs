using Digi.Shared.DTOs.admin.module;

namespace Digi.Shared.Services
{
    public interface IEmailTemplateService
    {
        Task<RenderedEmailTemplateDto> RenderAsync(
            int companyId,
            string templateCode,
            IReadOnlyDictionary<string, string> placeholders);
    }
}
