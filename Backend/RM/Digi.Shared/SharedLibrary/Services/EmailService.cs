using Digi.Shared.Helper;
using Digi.Shared.Services;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Digi.Shared.SharedLibrary.Services
{
    /// <summary>
    /// DEPRECATED legacy wrapper. All methods delegate to <see cref="Digi.Shared.Services.ICentralizedEmailService"/>.
    /// Inject <see cref="Digi.Shared.Services.ICentralizedEmailService"/> directly instead.
    /// </summary>
    [Obsolete("Use ICentralizedEmailService directly. This class is a thin wrapper kept for compatibility only.")]
    public class EmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ICentralizedEmailService _centralizedEmailService;

        // Constructor with configuration injection
        public EmailService(IConfiguration configuration, ICentralizedEmailService centralizedEmailService)
        {
            _smtpSettings = configuration.GetSection("Smtp").Get<SmtpSettings>();
            _centralizedEmailService = centralizedEmailService;
        }

        // Legacy method - now uses centralized service
        public async Task<ApiResponse<string>> SendEmailAsync(int companyId, string emailTo, string subject, string body, bool isHtml = true)
        {
            return await _centralizedEmailService.SendEmailAsync(companyId, emailTo, subject, body, isHtml);
        }

        // Legacy method - kept for backward compatibility
        public void SendEmail(string emailTo, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("DigiSoft", _smtpSettings.FromEmail));
            message.To.Add(new MailboxAddress("", emailTo));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body,
                TextBody = "Please enable HTML to view this email" // Fallback text
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // Connect with configured settings
                client.Connect(
                    _smtpSettings.Host,
                    int.Parse(_smtpSettings.Port),
                    _smtpSettings.EnableSSL.ToLower() == "true"
                        ? MailKit.Security.SecureSocketOptions.StartTls
                        : MailKit.Security.SecureSocketOptions.None);

                client.Authenticate(_smtpSettings.Username, _smtpSettings.Password);
                client.Send(message);
                Console.WriteLine($"Email sent to {emailTo}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                throw; // Re-throw for proper error handling
            }
            finally
            {
                client.Disconnect(true);
            }
        }
    }

    // Configuration model class
    public class SmtpSettings
    {
        public string Host { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EnableSSL { get; set; }
        public string FromEmail { get; set; }
    }
}
