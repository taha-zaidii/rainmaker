using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs
{
    public class CorsDto
    {
        public string[] AllowedOrigins { get; set; }
    }
    public class AppSettings
    {
        public string Domain { get; set; }
        public string? LoginUrl { get; set; }
        public string? BaseUrl { get; set; }
    }
}
