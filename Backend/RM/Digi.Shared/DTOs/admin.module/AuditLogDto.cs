using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.admin.module
{
    public class AuditLogListItemDto
    {
        public long AuditLogId { get; set; }
        public string Module { get; set; } = string.Empty;
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public string? HttpMethod { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public int? CompanyId { get; set; }
        public int? EmployeeId { get; set; }
        public string? IpAddress { get; set; }
        public string? ActionType { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? Status { get; set; }
        public long? DurationMs { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class AuditLogDetailDto : AuditLogListItemDto
    {
        public string? RequestUrl { get; set; }
        public string? MachineName { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? Description { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UserAgent { get; set; }
    }

    public class AuditLogFilterDto
    {
        public string? Module { get; set; }
        public string? ActionType { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public int? CompanyId { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 50;
    }

    public class AuditLogPagedResultDto
    {
        public List<AuditLogListItemDto> Data { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
