using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BatchProcessing.Core.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await SendEmailAsync(new List<string> { to }, subject, body);
    }

    public async Task SendEmailAsync(List<string> recipients, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Batch Processing System", _configuration["Email:FromEmail"] ?? "noreply@company.com"));
            
            foreach (var recipient in recipients)
            {
                message.To.Add(new MailboxAddress("", recipient));
            }

            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            
            // Для демонстрации используем MailHog (локальный SMTP сервер)
            var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "1025");
            
            await client.ConnectAsync(smtpHost, smtpPort, false);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Email отправлен успешно. Получатели: {string.Join(", ", recipients)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при отправке email. Получатели: {string.Join(", ", recipients)}");
            throw;
        }
    }
}
