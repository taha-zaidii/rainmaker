namespace Digi.Shared.DTOs.admin.module
{
    public class SubscriptionPackageCreateDto
    {
      //  public int? CompanyID { get; set; }
        public string? PackageName { get; set; }
        public string? PackageImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? CreatedBy { get; set; }

        //public int? MaxUsers { get; set; }
        //public int? ModuleID { get; set; }
        //public bool? AllPermission { get; set; }
        public List<PackageFeatureCreteUpdateDto> Features { get; set; } = new List<PackageFeatureCreteUpdateDto>();
    }

    public class SubscriptionPackageUpdateDto
    {
        public int PackageID { get; set; }
       // public int? CompanyID { get; set; }
        public string? PackageName { get; set; }
        public string? PackageImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? UpdatedBy { get; set; }
        //public int? MaxUsers { get; set; }
        //public int? ModuleID { get; set; }
        //public bool? AllPermission { get; set; }
        public List<PackageFeatureCreteUpdateDto> Features { get; set; } = new List<PackageFeatureCreteUpdateDto>();

    }

    public class PackageFeatureCreteUpdateDto
    {       
        public int NavID { get; set; }
        public int? ModuleID { get; set; }
        public bool? Permission { get; set; }
        public int? MaxUsers { get; set; }
        //  public int? PermissionID { get; set; }
    }

    public class SubscriptionPackageResponseDto
    {
        public int PackageID { get; set; }
       // public int? CompanyID { get; set; }
        public string? CompanyName { get; set; }
        public string? PackageName { get; set; }
        public string? PackageImageUrl { get; set; }
  
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public bool? IsActive { get; set; }
        public List<FeatureResponseDto> Features { get; set; } = new List<FeatureResponseDto>();
    }

    public class FeatureResponseDto
    {
        public int? FeatureID { get; set; }
        public int? PackageID { get; set; }
        public int? NavID { get; set; }
        public int? ParentID { get; set; }
        public string? DisplayName { get; set; }
        public int? PermissionID { get; set; }
        public string? PermissionName { get; set; }
        public int? ModuleID { get; set; }
        public string? ModuleName { get; set; }
        public int? MaxUsers { get; set; }
    }

}
