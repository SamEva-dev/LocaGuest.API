using LocaGuest.Application.Features.Inventories.Commands.SendInventoryEmail;
using LocaGuest.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace LocaGuest.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        List<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var smtpUser = _configuration["Email:SmtpUser"] ?? "";
        var smtpPass = _configuration["Email:SmtpPassword"] ?? "";
        var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
        var fromName = _configuration["Email:FromName"] ?? "LocaGuest";

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUser, smtpPass)
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(to);

        if (attachments != null)
        {
            foreach (var attachment in attachments)
            {
                var stream = new MemoryStream(attachment.Content);
                message.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
            }
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }
}
