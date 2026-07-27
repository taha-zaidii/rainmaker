using System.ComponentModel.DataAnnotations;
using static Digi.Shared.DTOs.ExtensionsDto;

namespace Digi.Shared.DTOs.gen.module
{
    public class DepartmentCreateDto
    {

        public string? DepartmentName { get; set; }
        public string? DepartmentEmail { get; set; }
        public string? Division { get; set; }
        public int? CompanyID { get; set; }
        public int? DepartmentHeadID { get; set; }

        public string? EmployeeCode { get; set; }
    }

    public class DepartmentUpdateDto : DepartmentCreateDto
    {
        public int? DepartmentID { get; set; }

    }
    
    public class DepartmentDto
    {
        public int DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public string? DepartmentEmail { get; set; }
        public string? Division { get; set; }
        public int? CompanyID { get; set; }
        public CompanyNameDto? Company { get; set; } = new CompanyNameDto();
        public int? DepartmentHeadID { get; set; }
        public string? EmployeeName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? EmployeeCode { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? IsDeletedOn { get; set; }
    }

    public class DepartmentalOrganogramNodeDto
    {
        public string? NodeID { get; set; }
        public string? ParentNodeID { get; set; }
        public string? NodeType { get; set; }
        public string? DisplayName { get; set; }
        public string? SubText { get; set; }

        public int CompanyID { get; set; }
        public string? Division { get; set; }
        public int? DepartmentID { get; set; }
        public int? EmployeeID { get; set; }
        public int? LineManagerID { get; set; }
        public int? ManagerEmployeeID { get; set; }
        public int? DesignationID { get; set; }
        public string? DesignationName { get; set; }
        public int? DepartmentCount { get; set; }
        public int? EmployeeCount { get; set; }
        public int? TreeLevel { get; set; }
        public int? SortOrder { get; set; }
        public int? SiblingSort { get; set; }
    }
}
