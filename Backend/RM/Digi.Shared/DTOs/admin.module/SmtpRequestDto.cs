using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.admin.module
{
    public class SmtpRequestDto
    {
        public string Action { get; set; }
        public int? SmtpId { get; set; }
        public int? CompanyId { get; set; }
        public string? MailProtocol { get; set; }
        public string? MailEncryption { get; set; }
        public string? MailHost { get; set; }
        public int? MailPort { get; set; }
        public string? MailUserName { get; set; }
        public string? MailPassword { get; set; }
        public bool? IsEnableSSL { get; set; }
        public bool? IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public bool? IsSuperAdmin { get; set; }  // 🔹 new flag
    }

    public class SmtpResponseDto
    {
        public int SMTPID { get; set; }
        public int? CompanyID { get; set; }
        public string? MailProtocol { get; set; }
        public string? MailEncryption { get; set; }
        public string? MailHost { get; set; }
        public int? MailPort { get; set; }
        public string? MailUserName { get; set; }
        public string? MailPassword { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public bool? IsSuperAdmin { get; set; }

        // Response codes
        public int Code { get; set; }
        public int Success { get; set; }
        public string Message { get; set; }
    }
}
