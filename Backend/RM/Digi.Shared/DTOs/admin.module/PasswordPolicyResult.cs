using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.admin.module
{
    public class PasswordPolicyResult
    {
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
    }

}
