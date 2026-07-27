using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.admin.module
{
    public class BranchDto
    {
        public int BranchID { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public int LocationID { get; set; }
        public int CompanyID { get; set; }
        public int AreaID { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedOn { get; set; }
      
    }


    public class CreateBranchRequest
    {
        public string BranchName { get; set; }
        public int? LocationID { get; set; }
        public int CompanyID { get; set; }
        public int? AreaID { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class UpdateBranchRequest : CreateBranchRequest
    {
        [Required(ErrorMessage = "Branch ID is required")]
        public int BranchID { get; set; }
    }

    public class DeleteBranchDto
    {
        public int BranchID { get; set; }
        public int CompanyID { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class ToggleBranchStatusDto
    {
        public int BranchID { get; set; }
        public int CompanyID { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class BulkDeleteBranchRequest
    {
        public List<DeleteBranchDto> Items { get; set; }
    }

    public class BulkDeleteBranchResult
    {
        public int Requested { get; set; }
        public int Deleted { get; set; }
        public int Skipped { get; set; }
    }


}
