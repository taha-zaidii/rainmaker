using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.notification
{
    // =============================================
    // NOTIFICATION DTOs
    // =============================================

    /// <summary>
    /// Base notification DTO
    /// </summary>
    public class NotificationDto
    {
        public long NotificationID { get; set; }
        public int CompanyID { get; set; }
        public int? TemplateID { get; set; }
        public int CategoryID { get; set; }
        public int RecipientID { get; set; }
        public string RecipientType { get; set; } = "User";
        public string RecipientEmail { get; set; }
        public string RecipientPhone { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string NotificationType { get; set; } = "InApp";
        public int Priority { get; set; } = 1;
        public string Status { get; set; } = "Pending";
        public DateTime? ScheduledTime { get; set; }
        public DateTime? SentTime { get; set; }
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        public string ErrorMessage { get; set; }
        public string Metadata { get; set; }
        public string SourceModule { get; set; }
        public string SourceAction { get; set; }
        public string SourceID { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadTime { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    /// <summary>
    /// Notification category DTO
    /// </summary>
    public class NotificationCategoryDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public string Description { get; set; }
        public int? ModuleID { get; set; }
        public string IconClass { get; set; }
        public string ColorCode { get; set; }
        public int Priority { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    /// <summary>
    /// Notification template DTO
    /// </summary>
    public class NotificationTemplateDto
    {
        public int TemplateID { get; set; }
        public string TemplateName { get; set; }
        public string TemplateCode { get; set; }
        public int CategoryID { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string TemplateType { get; set; }
        public string Variables { get; set; } // JSON array of template variables
        public bool IsHTML { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    /// <summary>
    /// Notification preferences DTO
    /// </summary>
    public class NotificationPreferencesDto
    {
        public int PreferenceID { get; set; }
        public int CompanyID { get; set; }
        public int UserID { get; set; }
        public int CategoryID { get; set; }
        public bool EmailEnabled { get; set; } = true;
        public bool SMSEnabled { get; set; } = false;
        public bool PushEnabled { get; set; } = true;
        public bool InAppEnabled { get; set; } = true;
        public string Frequency { get; set; } = "Immediate";
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    /// <summary>
    /// Notification subscription DTO
    /// </summary>
    public class NotificationSubscriptionDto
    {
        public int SubscriptionID { get; set; }
        public int CompanyID { get; set; }
        public int UserID { get; set; }
        public string SubscriptionType { get; set; }
        public string SubscriptionTarget { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    // =============================================
    // REQUEST DTOs
    // =============================================

    /// <summary>
    /// Create notification request DTO
    /// </summary>
    public class CreateNotificationRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        public int? TemplateID { get; set; }
        
        [Required]
        public int CategoryID { get; set; }
        
        [Required]
        public int RecipientID { get; set; }
        
        public string RecipientType { get; set; } = "User";
        public string RecipientEmail { get; set; }
        public string RecipientPhone { get; set; }
        
        [Required]
        public string Subject { get; set; }
        
        [Required]
        public string Message { get; set; }
        
        public string NotificationType { get; set; } = "InApp";
        public int Priority { get; set; } = 1;
        public DateTime? ScheduledTime { get; set; }
        public string Metadata { get; set; }
        public string SourceModule { get; set; }
        public string SourceAction { get; set; }
        public string SourceID { get; set; }
        
        [Required]
        public string CreatedBy { get; set; }
    }

    /// <summary>
    /// Update notification request DTO
    /// </summary>
    public class UpdateNotificationRequestDto
    {
        [Required]
        public long NotificationID { get; set; }
        
        public string Subject { get; set; }
        public string Message { get; set; }
        public int Priority { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string Metadata { get; set; }
        
        [Required]
        public string UpdatedBy { get; set; }
    }

    /// <summary>
    /// Get notifications request DTO
    /// </summary>
    public class GetNotificationsRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        public int PageSize { get; set; } = 20;
        public int PageNumber { get; set; } = 1;
        public bool? IsRead { get; set; }
        public int? CategoryID { get; set; }
        public int? Priority { get; set; }
    }

    /// <summary>
    /// Mark notification as read request DTO
    /// </summary>
    public class MarkAsReadRequestDto
    {
        [Required]
        public long NotificationID { get; set; }
        
        [Required]
        public int UserID { get; set; }
    }

    /// <summary>
    /// Bulk mark as read request DTO
    /// </summary>
    public class BulkMarkAsReadRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        public List<long> NotificationIDs { get; set; } = new List<long>();
        public int? CategoryID { get; set; }
        public bool MarkAll { get; set; } = false;
    }

    /// <summary>
    /// Update preferences request DTO
    /// </summary>
    public class UpdatePreferencesRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        public int CategoryID { get; set; }
        
        public bool EmailEnabled { get; set; } = true;
        public bool SMSEnabled { get; set; } = false;
        public bool PushEnabled { get; set; } = true;
        public bool InAppEnabled { get; set; } = true;
        public string Frequency { get; set; } = "Immediate";
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }
        
        [Required]
        public string UpdatedBy { get; set; }
    }

    /// <summary>
    /// Create subscription request DTO
    /// </summary>
    public class CreateSubscriptionRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        public string SubscriptionType { get; set; }
        
        [Required]
        public string SubscriptionTarget { get; set; }
        
        [Required]
        public string CreatedBy { get; set; }
    }

    // =============================================
    // RESPONSE DTOs
    // =============================================

    /// <summary>
    /// Notification response DTO with category info
    /// </summary>
    public class NotificationResponseDto
    {
        public long NotificationID { get; set; }
        public int RecipientID { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string NotificationType { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadTime { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Metadata { get; set; }
        
        // Category information
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public string IconClass { get; set; }
        public string ColorCode { get; set; }
    }

    /// <summary>
    /// Paginated notifications response DTO
    /// </summary>
    public class PaginatedNotificationsResponseDto
    {
        public List<NotificationResponseDto> Notifications { get; set; } = new List<NotificationResponseDto>();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// Notification statistics response DTO
    /// </summary>
    public class NotificationStatisticsResponseDto
    {
        public int TotalNotifications { get; set; }
        public int ReadNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int CriticalNotifications { get; set; }
        public int HighPriorityNotifications { get; set; }
        public List<CategoryStatisticsDto> CategoryStatistics { get; set; } = new List<CategoryStatisticsDto>();
    }

    /// <summary>
    /// Category statistics DTO
    /// </summary>
    public class CategoryStatisticsDto
    {
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public int Count { get; set; }
        public int UnreadCount { get; set; }
    }

    /// <summary>
    /// Template variable DTO
    /// </summary>
    public class TemplateVariableDto
    {
        public string VariableName { get; set; }
        public string VariableValue { get; set; }
        public string VariableType { get; set; } = "String";
    }

    /// <summary>
    /// Process template request DTO
    /// </summary>
    public class ProcessTemplateRequestDto
    {
        [Required]
        public int TemplateID { get; set; }
        
        public List<TemplateVariableDto> Variables { get; set; } = new List<TemplateVariableDto>();
    }

    /// <summary>
    /// Processed template response DTO
    /// </summary>
    public class ProcessedTemplateResponseDto
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsHTML { get; set; }
        public string TemplateType { get; set; }
    }

    // =============================================
    // HR MODULE SPECIFIC DTOs
    // =============================================

    /// <summary>
    /// HR notification request DTO
    /// </summary>
    public class HRNotificationRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        public string NotificationType { get; set; } // Employee_Created, Leave_Request, Interview_Scheduled, etc.
        
        [Required]
        public int RecipientID { get; set; }
        
        public string RecipientEmail { get; set; }
        public string RecipientPhone { get; set; }
        
        [Required]
        public string Subject { get; set; }
        
        [Required]
        public string Message { get; set; }
        
        public int Priority { get; set; } = 1;
        public DateTime? ScheduledTime { get; set; }
        public string Metadata { get; set; }
        public string SourceID { get; set; } // EmployeeID, LeaveID, InterviewID, etc.
        
        [Required]
        public string CreatedBy { get; set; }
    }

    /// <summary>
    /// Bulk HR notification request DTO
    /// </summary>
    public class BulkHRNotificationRequestDto
    {
        [Required]
        public int CompanyID { get; set; }
        
        [Required]
        public string NotificationType { get; set; }
        
        public List<int> RecipientIDs { get; set; } = new List<int>();
        public List<string> RecipientEmails { get; set; } = new List<string>();
        public List<string> RecipientPhones { get; set; } = new List<string>();
        
        [Required]
        public string Subject { get; set; }
        
        [Required]
        public string Message { get; set; }
        
        public int Priority { get; set; } = 1;
        public DateTime? ScheduledTime { get; set; }
        public string Metadata { get; set; }
        public string SourceID { get; set; }
        
        [Required]
        public string CreatedBy { get; set; }
    }
}
