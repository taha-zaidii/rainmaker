using Digi.Shared.DTOs.admin.module;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Logging;
using Digi.Shared.Helper;
using Digi.Shared.DTOs;

namespace Digi.Shared.Services
{
    public interface ICentralizedEmailService
    {
        Task<ApiResponse<string>> SendEmailAsync(int companyId, string toEmail, string subject, string body, bool isHtml = true);
        Task<bool> SendBulkEmailAsync(int companyId, List<string> toEmails, string subject, string body, bool isHtml = true);
        Task<ApiResponse<string>> SendEmaiwithAttachmentlAsync(int companyId, string toEmail, string subject, string body, bool isHtml = true, List<EmailAttachment>? attachments = null);
        Task<ApiResponse<string>> SendEmailWithCCAsync(int companyId, string toEmail, string subject, string body, List<string>? ccEmails = null, bool isHtml = true);
        Task<ApiResponse<string>> SendEmailWithCCAndBCCAsync(int companyId, string toEmail, string subject, string body, List<string>? ccEmails = null, List<string>? bccEmails = null, bool isHtml = true);
    }

    public class CentralizedEmailService : ICentralizedEmailService
    {
        private readonly ISmtpRepository _smtpRepository;
        private readonly ILogger<CentralizedEmailService> _logger;

        public CentralizedEmailService(ISmtpRepository smtpRepository, ILogger<CentralizedEmailService> logger)
        {
            _smtpRepository = smtpRepository;
            _logger = logger;
        }

        public async Task<ApiResponse<string>> SendEmailAsync(int companyId, string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                // Get SMTP configuration from database by CompanyID
                var smtpConfig = await _smtpRepository.GetSmtpByCompanyIdAsync(companyId);
                
                if (smtpConfig == null)
                {
                    _logger.LogError($"No SMTP configuration found for CompanyID: {companyId}");
                    return ApiResponse<string>.Fail($"No SMTP configuration found for Company.");
                }

                if (!smtpConfig.IsActive.HasValue || !smtpConfig.IsActive.Value)
                {
                    _logger.LogError($"SMTP configuration is not active for CompanyID: {companyId}");
                    return ApiResponse<string>.Fail($"SMTP configuration is not active for Company.");
                }

                // Create email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("DigiSoft ERP", smtpConfig.MailUserName));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = isHtml ? body : null,
                    TextBody = isHtml ? "Please enable HTML to view this email" : body
                };


                message.Body = bodyBuilder.ToMessageBody();

                // Send email using company-specific SMTP settings
                using var client = new SmtpClient();
                
                await client.ConnectAsync(
                    smtpConfig.MailHost,
                    smtpConfig.MailPort ?? 587,
                    smtpConfig.MailEncryption?.ToLower() == "ssl" 
                        ? MailKit.Security.SecureSocketOptions.SslOnConnect
                        : MailKit.Security.SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(smtpConfig.MailUserName, smtpConfig.MailPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {toEmail} using CompanyID: {companyId}");
                return ApiResponse<string>.Success($"Email sent successfully to {toEmail} using Company."); ;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail} for CompanyID: {companyId}");
                return ApiResponse<string>.Fail($"Failed to send email to {toEmail} for Company."); ;
            }
        }

        public async Task<bool> SendBulkEmailAsync(int companyId, List<string> toEmails, string subject, string body, bool isHtml = true)
        {
            try
            {
                // Get SMTP configuration from database by CompanyID
                var smtpConfig = await _smtpRepository.GetSmtpByCompanyIdAsync(companyId);
                
                if (smtpConfig == null)
                {
                    _logger.LogError($"No SMTP configuration found for CompanyID: {companyId}");
                    return false;
                }

                if (!smtpConfig.IsActive.HasValue || !smtpConfig.IsActive.Value)
                {
                    _logger.LogError($"SMTP configuration is not active for CompanyID: {companyId}");
                    return false;
                }

                // Create email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("DigiSoft ERP", smtpConfig.MailUserName));
                
                // Add all recipients
                foreach (var email in toEmails)
                {
                    message.To.Add(new MailboxAddress("", email));
                }
                
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = isHtml ? body : null,
                    TextBody = isHtml ? "Please enable HTML to view this email" : body
                };
                message.Body = bodyBuilder.ToMessageBody();

                // Send email using company-specific SMTP settings
                using var client = new SmtpClient();
                
                await client.ConnectAsync(
                    smtpConfig.MailHost,
                    smtpConfig.MailPort ?? 587,
                    smtpConfig.MailEncryption?.ToLower() == "ssl" 
                        ? MailKit.Security.SecureSocketOptions.SslOnConnect
                        : MailKit.Security.SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(smtpConfig.MailUserName, smtpConfig.MailPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Bulk email sent successfully to {toEmails.Count} recipients using CompanyID: {companyId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send bulk email for CompanyID: {companyId}");
                return false;
            }
        }

        public async Task<ApiResponse<string>> SendEmaiwithAttachmentlAsync(int companyId, string toEmail, string subject, string body, bool isHtml = true, List<EmailAttachment>? attachments = null)
        {
            try
            {
                // Get SMTP configuration from database by CompanyID
                var smtpConfig = await _smtpRepository.GetSmtpByCompanyIdAsync(companyId);

                if (smtpConfig == null)
                {
                    _logger.LogError($"No SMTP configuration found for CompanyID: {companyId}");
                    return ApiResponse<string>.Fail($"No SMTP configuration found for Company.");
                }

                if (!smtpConfig.IsActive.HasValue || !smtpConfig.IsActive.Value)
                {
                    _logger.LogError($"SMTP configuration is not active for CompanyID: {companyId}");
                    return ApiResponse<string>.Fail($"SMTP configuration is not active for Company.");
                }

                // Create email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("DigiSoft ERP", smtpConfig.MailUserName));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = isHtml ? body : null,
                    TextBody = isHtml ? "Please enable HTML to view this email" : body
                };

                if (attachments != null && attachments.Any())
                {
                    foreach (var attachment in attachments)
                    {
                        bodyBuilder.Attachments.Add(
                            attachment.FileName,
                            attachment.FileBytes,
                            ContentType.Parse(attachment.ContentType)
                        );
                    }
                }

                message.Body = bodyBuilder.ToMessageBody();

                // Send email using company-specific SMTP settings
                using var client = new SmtpClient();

                await client.ConnectAsync(
                    smtpConfig.MailHost,
                    smtpConfig.MailPort ?? 587,
                    smtpConfig.MailEncryption?.ToLower() == "ssl"
                        ? MailKit.Security.SecureSocketOptions.SslOnConnect
                        : MailKit.Security.SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(smtpConfig.MailUserName, smtpConfig.MailPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {toEmail} using CompanyID: {companyId}");
                return ApiResponse<string>.Success($"Email sent successfully to {toEmail} using Company."); ;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail} for CompanyID: {companyId}");
                return ApiResponse<string>.Fail($"Failed to send email to {toEmail} for Company."); ;
            }
        }

        public async Task<ApiResponse<string>> SendEmailWithCCAsync(int companyId, string toEmail, string subject, string body, List<string>? ccEmails = null, bool isHtml = true)
        {
            try
            {
                // Get SMTP configuration from database by CompanyID
                var smtpConfig = await _smtpRepository.GetSmtpByCompanyIdAsync(companyId);
                
                if (smtpConfig == null)
                {
                    _logger.LogError($"No SMTP configuration found for CompanyID: {companyId}");
                    return ApiResponse<string>.Fail($"No SMTP configuration found for Company.");
                }

                if (!smtpConfig.IsActive.HasValue || !smtpConfig.IsActive.Value)
                {
                    _logger.LogError($"SMTP configuration is not active for CompanyID: {companyId}");
                    return ApiResponse<string>.Fail($"SMTP configuration is not active for Company.");
                }

                // Create email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("DigiSoft ERP", smtpConfig.MailUserName));
                message.To.Add(new MailboxAddress("", toEmail));
                
                // Add CC emails if provided
                if (ccEmails != null && ccEmails.Any())
                {
                    foreach (var ccEmail in ccEmails)
                    {
                        if (!string.IsNullOrWhiteSpace(ccEmail))
                        {
                            message.Cc.Add(new MailboxAddress("", ccEmail.Trim()));
                        }
                    }
                }
                
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = isHtml ? body : null,
                    TextBody = isHtml ? "Please enable HTML to view this email" : body
                };

                message.Body = bodyBuilder.ToMessageBody();

                // Send email using company-specific SMTP settings
                using var client = new SmtpClient();
                
                await client.ConnectAsync(
                    smtpConfig.MailHost,
                    smtpConfig.MailPort ?? 587,
                    smtpConfig.MailEncryption?.ToLower() == "ssl" 
                        ? MailKit.Security.SecureSocketOptions.SslOnConnect
                        : MailKit.Security.SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(smtpConfig.MailUserName, smtpConfig.MailPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                var ccInfo = ccEmails != null && ccEmails.Any() ? $" with CC to {string.Join(", ", ccEmails)}" : "";
                _logger.LogInformation($"Email sent successfully to {toEmail}{ccInfo} using CompanyID: {companyId}");
                return ApiResponse<string>.Success($"Email sent successfully to {toEmail}{ccInfo} using Company.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail} for CompanyID: {companyId}");
                return ApiResponse<string>.Fail($"Failed to send email to {toEmail} for Company.");
            }
        }

        public async Task<ApiResponse<string>> SendEmailWithCCAndBCCAsync(int companyId, string toEmail, string subject, string body, List<string>? ccEmails = null, List<string>? bccEmails = null, bool isHtml = true)
        {
            try
            {
                var smtpConfig = await _smtpRepository.GetSmtpByCompanyIdAsync(companyId);
                if (smtpConfig == null)
                {
                    _logger.LogError($"No SMTP configuration found for CompanyID: {companyId}");
                    return ApiResponse<string>.Fail($"No SMTP configuration found for Company.");
                }
                if (!smtpConfig.IsActive.HasValue || !smtpConfig.IsActive.Value)
                {
                    _logger.LogError($"SMTP configuration is not active for CompanyID: {companyId}");
                    return ApiResponse<string>.Fail($"SMTP configuration is not active for Company.");
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("DigiSoft ERP", smtpConfig.MailUserName));
                message.To.Add(new MailboxAddress("", toEmail));

                if (ccEmails != null && ccEmails.Any())
                {
                    foreach (var ccEmail in ccEmails)
                    {
                        if (!string.IsNullOrWhiteSpace(ccEmail))
                            message.Cc.Add(new MailboxAddress("", ccEmail.Trim()));
                    }
                }
                if (bccEmails != null && bccEmails.Any())
                {
                    foreach (var bccEmail in bccEmails)
                    {
                        if (!string.IsNullOrWhiteSpace(bccEmail))
                            message.Bcc.Add(new MailboxAddress("", bccEmail.Trim()));
                    }
                }

                message.Subject = subject;
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = isHtml ? body : null,
                    TextBody = isHtml ? "Please enable HTML to view this email" : body
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    smtpConfig.MailHost,
                    smtpConfig.MailPort ?? 587,
                    smtpConfig.MailEncryption?.ToLower() == "ssl"
                        ? MailKit.Security.SecureSocketOptions.SslOnConnect
                        : MailKit.Security.SecureSocketOptions.StartTls
                );
                await client.AuthenticateAsync(smtpConfig.MailUserName, smtpConfig.MailPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                var ccInfo = ccEmails != null && ccEmails.Any() ? $" CC: {string.Join(", ", ccEmails)}" : "";
                var bccInfo = bccEmails != null && bccEmails.Any() ? $" BCC: {string.Join(", ", bccEmails)}" : "";
                _logger.LogInformation($"Email sent successfully to {toEmail}{ccInfo}{bccInfo} using CompanyID: {companyId}");
                return ApiResponse<string>.Success($"Email sent successfully to {toEmail} using Company.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail} for CompanyID: {companyId}");
                return ApiResponse<string>.Fail($"Failed to send email to {toEmail} for Company.");
            }
        }
    }
}
