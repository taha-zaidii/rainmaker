using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class BrandDto
    {
        public int BrandID { get; set; }
        public string BrandCode { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? BrandImageUrl { get; set; }
        public int? AttachmentDetailID { get; set; }
        public IFormFile? AttachmentFile { get; set; }
        public string? AttachmentURL { get; set; }
        public string? CompanyName { get; set; }
    }

    public class CreateBrandRequest
    {

        [Required(ErrorMessage = "Brand Name is required")]
        public string BrandName { get; set; } = string.Empty;
        public string? ContactPerson { get; set; } = string.Empty;
        public string? Phone { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? Website { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;        
        public string? Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int CompanyID { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? BrandImageUrl { get; set; }
        public IFormFile? AttachmentFile { get; set; }
        public string? AttachmentURL { get; set; }
        public string? CompanyName { get; set; }
        public bool RemoveImage { get; set; } = false;
    }

    public class UpdateBrandRequest : CreateBrandRequest
    {
        [Required(ErrorMessage = "Brand ID is required")]
        public int BrandID { get; set; }
        public int? AttachmentDetailID { get; set; }
    }

    public class deleteBrandDto
    {
        public int BrandID { get; set; }
        public int CompanyID { get; set; }
        public string? UpdatedBy { get; set; } = string.Empty;
    }

    public class BulkDeleteBrandsRequest
    {
        public List<deleteBrandDto> Items { get; set; } = new();
    }

    public class BulkDeleteBrandsResult
    {
        public int Requested { get; set; }
        public int Deleted { get; set; }
        public int Skipped { get; set; } // reserved for future (e.g., when skipping brands with items)
        public List<int> DeletedBrandIDs { get; set; } = new();
        public List<int> SkippedBrandIDs { get; set; } = new();
    }
}