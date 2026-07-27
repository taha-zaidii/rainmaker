namespace Digi.Shared.SharedLibrary.Interfaces
{
    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);

    }
}
