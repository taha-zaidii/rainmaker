using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.hrm.module
{
    public class ResignationDetailDto
    {
        public int? ResignationDetailID { get; set; }
        public int CompanyID { get; set; }
        public string DetailTitle { get; set; }
        public int CreatedBy { get; set; }
    }

}
