using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.admin.module
{
    public class SubscriptionFeaturesCreateDto
    {
        public int? PackageID { get; set; }
        public int? ModuleID { get; set; }
        public int? NavID { get; set; }
        public int? MaxUsers { get; set; }
        public string? FeatureName { get; set; }
        public string? Description { get; set; }
        public string? CreatedBy { get; set; }
        public bool? IsActive { get; set; }
  
    }

    public class SubscriptionFeaturesUpdateDto
    {
        public int? PackageID { get; set; }

        public int? ModuleID { get; set; }
        public int? NavID { get; set; }
        public int? MaxUsers { get; set; }

        public string? FeatureName { get; set; }
        public string? Description { get; set; }
        public string? UpdatedBy { get; set; }
        public bool? IsActive { get; set; }

    }

    public class SubscriptionFeaturesDto
    {
        public int FeatureID { get; set; }
        public int? PackageID { get; set; }
        public PackageNameDto PackageName { get; set; } = new PackageNameDto();
        public int? ModuleID { get; set; }
        public ModuleNameDto? ModuleName { get; set; } = new ModuleNameDto();

        public int? NavID { get; set; } // For NavID in ModuleNameDto
        public NavNameDto NavName { get; set; } = new NavNameDto();

        public string? FeatureName { get; set; }
        public string? Description { get; set; } 
        public int? MaxUsers { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; } 
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? IsDeletedOn { get; set; }
        public bool? IsDeleted { get; set; }
    }
   
}
