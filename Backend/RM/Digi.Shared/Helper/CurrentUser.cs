using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.Helper
{
    public class CurrentUser
    {
        public string? UserID { get; set; }
        public string? CompanyID { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? EmployeeCode { get; set; }
        public string? Role { get; set; }
        public List<string>? Permissions { get; set; }

        public static CurrentUser FromClaims(ClaimsPrincipal user)
        {
            return new CurrentUser
            {
                UserID = user.FindFirst("UserID")?.Value!,
                CompanyID = user.FindFirst("CompanyID")?.Value!,
                UserName = user.FindFirst("UserName")?.Value!,
                Email = user.FindFirst("Email")?.Value!,
                EmployeeCode = user.FindFirst("EmployeeCode")?.Value!,
                Role = user.FindFirst("Role")?.Value!,
                Permissions = user.Claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList()
            };
        }
    }

}
