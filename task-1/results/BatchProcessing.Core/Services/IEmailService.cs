namespace BatchProcessing.Core.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendEmailAsync(List<string> recipients, string subject, string body);
}
