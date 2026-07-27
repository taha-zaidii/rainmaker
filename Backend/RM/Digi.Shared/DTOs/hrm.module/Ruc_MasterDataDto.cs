using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.hrm.module
{
    public class ClusterDto
    {
        public int ClusterID { get; set; }
        public string ClusterCode { get; set; }
        public string? ClusterName { get; set; }
        public string Description { get; set; }
        public int CompanyID { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsActive { get; set; }
    }
    public class JobCategoryDto
    {
        public int? ID { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int CompanyID { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        //public int? ID { get; set; }
        //public string? JobCategoryCode { get; set; }
        //public string? Name { get; set; }
        //public int? ClusterID { get; set; }
        //public string? ClusterName { get; set; }
        //public string? Description { get; set; }
        //public string? CreatedBy { get; set; }
        //public string? UpdatedBy { get; set; }
        //public bool IsActive { get; set; }
    }
    public class JobDescriptionDto
    {
        public int JobDescriptionID { get; set; }
        public string? JobCode { get; set; }
        public string? JobTitle { get; set; }
        public int? JobCategoryID { get; set; }
        public string? JobCategoryName { get; set; }
        public int? GradeID { get; set; }
        public string? GradeName { get; set; }
        public string? Responsibilities { get; set; }
        public string? Qualifications { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsSystemDefault { get; set; }
        public bool IsActive { get; set; }
    }
    public class GradeDto
    {
        public int? ID { get; set; }
        public string? Name { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public int CompanyID { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        //public int GradeID { get; set; }
        //public string GradeCode { get; set; }
        public string? Code { get; set; }
        public string? ColorCode { get; set; }

        //public string GradeName { get; set; }
        //public decimal SalaryRangeMin { get; set; }
        //public decimal SalaryRangeMax { get; set; }
        //public int CreatedBy { get; set; }
        //public int? UpdatedBy { get; set; }
        //public bool IsActive { get; set; }
    }
}
