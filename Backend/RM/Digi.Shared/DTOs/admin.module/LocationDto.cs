using Digi.Shared.DTOs.inventory.module;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.admin.module
{
    public class LocationDto
    {
        public int LocationID { get; set; }
        public int AreaID { get; set; }
        public string LocationName { get; set; }
        public string LocationCode { get; set; }
        public string Description { get; set; }
        public int CompanyID { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class CreateLocationRequest
    {
        public string LocationName { get; set; }
        public int? AreaID { get; set; }
        public int CompanyID { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set;} = string.Empty;
    }

    public class UpdateLocationRequest : CreateLocationRequest
    {
        [Required(ErrorMessage = "Location ID is required")]
        public int LocationID { get; set; }
    }

    public class DeleteLocationDto
    {
        public int LocationID { get; set; }
        public int CompanyID { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class ToggleLocationStatusDto
    {
        public int LocationID { get; set; }
        public int CompanyID { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class BulkDeleteLocationRequest
    {
        public List<DeleteLocationDto> Items { get; set; }
    }

    public class BulkDeleteLocationResult
    {
        public int Requested { get; set; }
        public int Deleted { get; set; }
        public int Skipped { get; set; }
    }

}
