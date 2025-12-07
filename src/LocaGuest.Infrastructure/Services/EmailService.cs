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

    public async Task SendTeamInvitationEmailAsync(
        string toEmail,
        string invitationToken,
        string organizationName,
        string inviterName,
        string role,
        CancellationToken cancellationToken = default)
    {
        var appUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:4200";
        var acceptUrl = $"{appUrl}/accept-invitation?token={invitationToken}";

        var roleLabel = GetRoleLabel(role);
        
        var subject = $"Invitation à rejoindre {organizationName} sur LocaGuest";
        var body = GetTeamInvitationTemplate(toEmail, organizationName, inviterName, roleLabel, acceptUrl);

        await SendEmailAsync(toEmail, subject, body, null, cancellationToken);
        _logger.LogInformation("Team invitation email sent to {Email} for organization {Organization}", 
            toEmail, organizationName);
    }

    public async Task SendTeamInvitationAcceptedEmailAsync(
        string toEmail,
        string memberName,
        string organizationName,
        CancellationToken cancellationToken = default)
    {
        var subject = $"{memberName} a rejoint votre équipe";
        var body = GetInvitationAcceptedTemplate(memberName, organizationName);

        await SendEmailAsync(toEmail, subject, body, null, cancellationToken);
        _logger.LogInformation("Invitation accepted notification sent to {Email}", toEmail);
    }

    private string GetRoleLabel(string role) => role switch
    {
        "Owner" => "Propriétaire",
        "Admin" => "Administrateur",
        "Manager" => "Gestionnaire",
        "Accountant" => "Comptable",
        "Viewer" => "Lecture seule",
        _ => role
    };

    private string GetTeamInvitationTemplate(
        string toEmail,
        string organizationName,
        string inviterName,
        string roleLabel,
        string acceptUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Invitation à rejoindre {organizationName}</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 40px 20px;"">
                <table role=""presentation"" style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #10b981 0%, #14b8a6 100%); padding: 40px 40px 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 28px; font-weight: 700;"">
                                🎉 Vous êtes invité !
                            </h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px;"">
                            <p style=""margin: 0 0 20px; color: #334155; font-size: 16px; line-height: 1.6;"">
                                Bonjour,
                            </p>
                            
                            <p style=""margin: 0 0 20px; color: #334155; font-size: 16px; line-height: 1.6;"">
                                <strong>{inviterName}</strong> vous invite à rejoindre l'organisation <strong>{organizationName}</strong> sur LocaGuest.
                            </p>
                            
                            <div style=""background-color: #f1f5f9; border-left: 4px solid #10b981; padding: 16px; margin: 24px 0; border-radius: 4px;"">
                                <p style=""margin: 0; color: #475569; font-size: 14px;"">
                                    <strong>Votre rôle :</strong> {roleLabel}
                                </p>
                            </div>
                            
                            <p style=""margin: 24px 0; color: #334155; font-size: 16px; line-height: 1.6;"">
                                En acceptant cette invitation, vous pourrez collaborer avec l'équipe et accéder aux fonctionnalités de gestion immobilière de LocaGuest.
                            </p>
                            
                            <!-- CTA Button -->
                            <table role=""presentation"" style=""margin: 32px 0;"">
                                <tr>
                                    <td style=""text-align: center;"">
                                        <a href=""{acceptUrl}"" style=""display: inline-block; padding: 16px 32px; background: linear-gradient(135deg, #10b981 0%, #14b8a6 100%); color: #ffffff; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px; box-shadow: 0 4px 6px rgba(16, 185, 129, 0.3);"">
                                            Accepter l'invitation
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style=""margin: 24px 0 0; color: #64748b; font-size: 14px; line-height: 1.6;"">
                                Ou copiez-collez ce lien dans votre navigateur :<br>
                                <a href=""{acceptUrl}"" style=""color: #10b981; word-break: break-all;"">{acceptUrl}</a>
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8fafc; padding: 24px 40px; border-radius: 0 0 12px 12px; border-top: 1px solid #e2e8f0;"">
                            <p style=""margin: 0 0 8px; color: #64748b; font-size: 13px; text-align: center;"">
                                Cette invitation a été envoyée à {toEmail}
                            </p>
                            <p style=""margin: 0; color: #94a3b8; font-size: 12px; text-align: center;"">
                                Si vous n'attendiez pas cette invitation, vous pouvez ignorer cet email.
                            </p>
                            <p style=""margin: 16px 0 0; color: #94a3b8; font-size: 12px; text-align: center;"">
                                © 2024 LocaGuest - Gestion immobilière simplifiée
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetInvitationAcceptedTemplate(string memberName, string organizationName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Nouveau membre dans votre équipe</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 40px 20px;"">
                <table role=""presentation"" style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); padding: 40px 40px 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 28px; font-weight: 700;"">
                                ✅ Nouveau membre
                            </h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px;"">
                            <p style=""margin: 0 0 20px; color: #334155; font-size: 16px; line-height: 1.6;"">
                                Bonne nouvelle !
                            </p>
                            
                            <p style=""margin: 0 0 20px; color: #334155; font-size: 16px; line-height: 1.6;"">
                                <strong>{memberName}</strong> a accepté votre invitation et a rejoint l'équipe de <strong>{organizationName}</strong>.
                            </p>
                            
                            <div style=""background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 16px; margin: 24px 0; border-radius: 4px;"">
                                <p style=""margin: 0; color: #475569; font-size: 14px;"">
                                    💡 Vous pouvez maintenant collaborer ensemble sur LocaGuest.
                                </p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8fafc; padding: 24px 40px; border-radius: 0 0 12px 12px; border-top: 1px solid #e2e8f0;"">
                            <p style=""margin: 0; color: #94a3b8; font-size: 12px; text-align: center;"">
                                © 2024 LocaGuest - Gestion immobilière simplifiée
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
