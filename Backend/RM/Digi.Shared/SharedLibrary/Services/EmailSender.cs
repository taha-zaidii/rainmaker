using System.Net.Mail;
using System.Net;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Digi.Shared.SharedLibrary.Services
{
    public class EmailSender : IEmailSender
    {


        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient(_configuration["Smtp:Host"])
                {
                    Port = int.Parse(_configuration["Smtp:Port"]),
                    Credentials = new NetworkCredential(
                        _configuration["Smtp:Username"],
                        _configuration["Smtp:Password"]
                    ),
                    EnableSsl = bool.Parse(_configuration["Smtp:EnableSSL"])
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(_configuration["Smtp:FromEmail"]),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false,
                };
                mail.To.Add(toEmail);

                await smtpClient.SendMailAsync(mail);
                return true;
            }
            catch
            {

                return false;
            }
        }

    }
}
