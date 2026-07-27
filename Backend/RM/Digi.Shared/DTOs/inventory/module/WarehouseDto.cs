using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class WarehouseDto
    {
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int? CityID { get; set; } 
        public int? StateID { get; set; }
        public int? LocationID { get; set; }
        public int? CountryID { get; set; }
        public int? AreaID { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string ManagerPhone { get; set; } = string.Empty;
        public string ManagerEmail { get; set; } = string.Empty;
        public int? TotalCapacity { get; set; }
        public int? MaxProducts { get; set; }
        public string OperatingHours { get; set; } = string.Empty;
        public int? WarehouseType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateWarehouseRequest
    {
        [Required(ErrorMessage = "Warehouse Name is required")]
        public string WarehouseName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Warehouse Code is required")]
        //public string WarehouseCode { get; set; } = string.Empty;
        public int CompanyID { get; set; }
        public int Location { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int? City { get; set; } 
        public int? State { get; set; } 
        public int? Country { get; set; }
        public int? Area { get; set; }
        public string? PostalCode { get; set; } = string.Empty;
        public string? ManagerName { get; set; } = string.Empty;
        public string? ManagerPhone { get; set; } = string.Empty;
        public string? ManagerEmail { get; set; } = string.Empty;
        public int? TotalCapacity { get; set; }
        public int? MaxProducts { get; set; }
        public string? OperatingHours { get; set; } = string.Empty;
        public int? WarehouseType { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;


    }

    public class DeleteWarehouseDto
    {
        public int WarehouseID { get; set; }
        public int CompanyID { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class ToggleWarehouseStatusDto
    {
        public int WarehouseID { get; set; }
        public int CompanyID { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class BulkDeleteWarehouseRequest
    {
        public List<DeleteWarehouseDto> Items { get; set; }
    }

    public class BulkDeleteWarehouseResult
    {
        public int Requested { get; set; }
        public int Deleted { get; set; }
        public int Skipped { get; set; }
    }

    public class UpdateWarehouseRequest : CreateWarehouseRequest
    {
        [Required(ErrorMessage = "Warehouse ID is required")]
        public int WarehouseID { get; set; }
    }

    public class WarehouseLocationDto
    {
        public int LocationID { get; set; }
        public int WarehouseID { get; set; }
        public string LocationCode { get; set; }
        public string LocationName { get; set; }
        public string? Aisle { get; set; }
        public string? Rack { get; set; }
        public string? Shelf { get; set; }
        public string? Bin { get; set; }

        public int CompanyID { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? IsDeletedOn { get; set; }

        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
    }


    public class CreateWarehouseLocationRequest
    {
        public int WarehouseID { get; set; }
        public string LocationName { get; set; }
        public string? Aisle { get; set; }
        public string? Rack { get; set; }
        public string? Shelf { get; set; }
        public string? Bin { get; set; }
        public int CompanyID { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class UpdateWarehouseLocationRequest : CreateWarehouseLocationRequest
    {
        [Required(ErrorMessage = "Location ID is required")]
        public int LocationID { get; set; }
    }


}
