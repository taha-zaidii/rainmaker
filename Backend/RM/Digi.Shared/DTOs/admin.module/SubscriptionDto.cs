using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.admin.module
{
    public class SubscriptionDto
    {
        public int CompanyId { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
