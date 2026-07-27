using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.inventory.module
{
    public class CategoryDto
    {
        public int CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryCode { get; set; }
        public string? Description { get; set; }
        public int? ParentCategoryID { get; set; }
        public string? ParentCategoryName { get; set; }
        public int? ItemClassID { get; set; }
        public string? ItemClassName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public string? CategoryImageUrl { get; set; }
        public int? AttachmentDetailID { get; set; }
        public string? CompanyName { get; set; }
    }

    public class CreateCategoryRequest
    {
        [Required(ErrorMessage = "Category Name is required")]
        public string CategoryName { get; set; }
        public string? CategoryCode { get; set; }
        public string? Description { get; set; }
        public int CompanyID { get; set; }
        //public bool IsActive { get; set; }
        public int ItemClassID { get; set; }
        public int? ParentCategoryID { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public IFormFile? AttachmentFile { get; set; }
        public string? AttachmentURL { get; set; }
        public string? CategoryImageUrl { get; set; }
        public string? CompanyName { get; set; }
        public bool RemoveImage { get; set; } = false;
    }

    public class UpdateCategoryRequest : CreateCategoryRequest
    {
        [Required(ErrorMessage = "Category ID is required")]
        public int CategoryID { get; set; }
        public int? AttachmentDetailID { get; set; }
    }

    public class BulkDeleteCategoriesRequest
    {
        public List<deleteCategoryDto> Items { get; set; } = new();
    }

    public class deleteCategoryDto
    {
        public int CategoryID { get; set; }
        public int CompanyID { get; set; }
        public string? UpdatedBy { get; set; } = string.Empty;
    }

    public class BulkDeleteCategoryResult
    {
        public int Requested { get; set; }
        public int Deleted { get; set; }
        public int Blocked { get; set; }

        public List<BlockedParentDto> BlockedParents { get; set; } = new();
    }

    public class BlockedParentDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public int CompanyID { get; set; }
    }

    public sealed class CategoryHierarchyDto
    {
        public int ParentCategoryID { get; set; }
        public string? ParentCategoryName { get; set; }
        public string? ParentCategoryCode { get; set; }
        public int ChildCategoryID { get; set; }
        public string? ChildCategoryName { get; set; }
        public string? ChildCategoryCode { get; set; }
    }

    public class CategoryChildDto
    {
        public int CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryCode { get; set; }
    }

    public class CategoryParentWithChildrenDto
    {
        public int ParentCategoryID { get; set; }
        public string? ParentCategoryName { get; set; }
        public string? ParentCategoryCode { get; set; }
        public List<CategoryChildDto> Children { get; set; } = new();
    }
}