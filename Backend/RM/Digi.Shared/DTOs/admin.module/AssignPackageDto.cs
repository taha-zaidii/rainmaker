using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.admin.module
{
    public class AssignPackageDto
    {
        public int CompanyID { get; set; }
        public int PackageID { get; set; }
        public string? UpdatedBy { get; set; } = string.Empty;
        public bool UpdateMaxUsers { get; set;} = false;
        public List<FeatureUpdateDto>? MaxUserUpdates { get; set; }
    }
    public class FeatureUpdateDto
    {
        public int? ModuleID { get; set; }
        public int? MaxUsers { get; set; }
    }


}
