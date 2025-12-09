using LocaGuest.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace LocaGuest.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        if (!_emailSettings.EnableEmailNotifications)
        {
            _logger.LogInformation("Email notifications disabled. Skipping email to {To}", to);
            return;
        }

        try
        {
            using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                EnableSsl = _emailSettings.EnableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    public async Task SendEmailAsync(string to, string subject, string body, List<LocaGuest.Application.Features.Inventories.Commands.SendInventoryEmail.EmailAttachment>? attachments = null, CancellationToken cancellationToken = default)
    {
        if (!_emailSettings.EnableEmailNotifications)
        {
            _logger.LogInformation("Email notifications disabled. Skipping email to {To}", to);
            return;
        }

        try
        {
            using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                EnableSsl = _emailSettings.EnableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    var stream = new MemoryStream(attachment.Content);
                    mailMessage.Attachments.Add(new System.Net.Mail.Attachment(stream, attachment.FileName, attachment.ContentType));
                }
            }

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    public async Task SendTeamInvitationEmailAsync(string toEmail, string invitationToken, string organizationName, string inviterName, string role, CancellationToken cancellationToken = default)
    {
        var subject = $"Invitation à rejoindre {organizationName} sur LocaGuest";
        var body = $@"
            <h2>Invitation à rejoindre une équipe</h2>
            <p>Bonjour,</p>
            <p><strong>{inviterName}</strong> vous invite à rejoindre <strong>{organizationName}</strong> sur LocaGuest en tant que <strong>{role}</strong>.</p>
            <p>Token d'invitation : <code>{invitationToken}</code></p>
            <p>Cordialement,<br/>L'équipe LocaGuest</p>";
        await SendEmailAsync(toEmail, subject, GetBaseTemplate(body), true);
    }

    public async Task SendTeamInvitationAcceptedEmailAsync(string toEmail, string memberName, string organizationName, CancellationToken cancellationToken = default)
    {
        var subject = $"{memberName} a rejoint {organizationName}";
        var body = $@"
            <h2>Nouveau membre</h2>
            <p>Bonjour,</p>
            <p><strong>{memberName}</strong> a accepté votre invitation et a rejoint <strong>{organizationName}</strong>.</p>
            <p>Cordialement,<br/>L'équipe LocaGuest</p>";
        await SendEmailAsync(toEmail, subject, GetBaseTemplate(body), true);
    }

    public async Task SendPaymentReceivedEmailAsync(string to, string tenantName, decimal amount, DateTime paymentDate)
    {
        var subject = "✅ Paiement reçu - LocaGuest";
        var body = GetPaymentReceivedTemplate(tenantName, amount, paymentDate);
        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    public async Task SendPaymentOverdueEmailAsync(string to, string tenantName, decimal amount, int daysLate, DateTime dueDate)
    {
        var subject = "⚠️ Paiement en retard - LocaGuest";
        var body = GetPaymentOverdueTemplate(tenantName, amount, daysLate, dueDate);
        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    public async Task SendPaymentReminderEmailAsync(string to, string tenantName, decimal amount, DateTime dueDate, int daysUntilDue)
    {
        var subject = "🔔 Rappel de paiement - LocaGuest";
        var body = GetPaymentReminderTemplate(tenantName, amount, dueDate, daysUntilDue);
        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    public async Task SendContractExpiringEmailAsync(string to, string tenantName, string propertyAddress, DateTime endDate, int daysUntilExpiry)
    {
        var subject = "📅 Votre bail arrive à échéance - LocaGuest";
        var body = GetContractExpiringTemplate(tenantName, propertyAddress, endDate, daysUntilExpiry);
        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    public async Task SendContractRenewalEmailAsync(string to, string tenantName, string propertyAddress, DateTime currentEndDate)
    {
        var subject = "🔄 Proposition de renouvellement de bail - LocaGuest";
        var body = GetContractRenewalTemplate(tenantName, propertyAddress, currentEndDate);
        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    public async Task SendMaintenanceScheduledEmailAsync(string to, string propertyAddress, string description, DateTime scheduledDate)
    {
        var subject = "🔧 Intervention planifiée - LocaGuest";
        var body = GetMaintenanceScheduledTemplate(propertyAddress, description, scheduledDate);
        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    public async Task SendMaintenanceCompletedEmailAsync(string to, string propertyAddress, string description, DateTime completedDate)
    {
        var subject = "✅ Intervention terminée - LocaGuest";
        var body = GetMaintenanceCompletedTemplate(propertyAddress, description, completedDate);
        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    public async Task SendDocumentUploadedEmailAsync(string to, string documentName, string uploadedBy, DateTime uploadedDate)
    {
        var subject = "📄 Nouveau document disponible - LocaGuest";
        var body = GetDocumentUploadedTemplate(documentName, uploadedBy, uploadedDate);
        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    public async Task SendWelcomeEmailAsync(string to, string firstName)
    {
        var subject = "🎉 Bienvenue sur LocaGuest !";
        var body = GetWelcomeTemplate(firstName);
        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    public async Task SendPasswordChangedEmailAsync(string to, string firstName)
    {
        var subject = "🔒 Votre mot de passe a été modifié - LocaGuest";
        var body = GetPasswordChangedTemplate(firstName);
        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    #region Email Templates

    private string GetBaseTemplate(string content)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 30px; color: #333; }}
        .content p {{ line-height: 1.6; margin-bottom: 15px; }}
        .highlight {{ background: #f0f7ff; border-left: 4px solid #667eea; padding: 15px; margin: 20px 0; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ background: #f9f9f9; padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #667eea; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .success {{ background: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>LocaGuest</h1>
        </div>
        <div class='content'>
            {content}
        </div>
        <div class='footer'>
            <p>© 2024 LocaGuest. Tous droits réservés.</p>
            <p>Cet email a été envoyé automatiquement, merci de ne pas y répondre.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetPaymentReceivedTemplate(string tenantName, decimal amount, DateTime paymentDate)
    {
        var content = $@"
            <h2>Paiement reçu ✅</h2>
            <p>Bonjour {tenantName},</p>
            <p>Nous avons bien reçu votre paiement.</p>
            <div class='success'>
                <p><strong>Montant :</strong> <span class='amount'>{amount:C}</span></p>
                <p><strong>Date :</strong> {paymentDate:dd/MM/yyyy}</p>
            </div>
            <p>Merci pour votre ponctualité !</p>
            <p>Cordialement,<br>L'équipe LocaGuest</p>";
        return GetBaseTemplate(content);
    }

    private string GetPaymentOverdueTemplate(string tenantName, decimal amount, int daysLate, DateTime dueDate)
    {
        var content = $@"
            <h2>Paiement en retard ⚠️</h2>
            <p>Bonjour {tenantName},</p>
            <p>Nous vous informons qu'un paiement est en retard de <strong>{daysLate} jour(s)</strong>.</p>
            <div class='warning'>
                <p><strong>Montant dû :</strong> <span class='amount'>{amount:C}</span></p>
                <p><strong>Date d'échéance :</strong> {dueDate:dd/MM/yyyy}</p>
            </div>
            <p>Merci de régulariser votre situation dans les plus brefs délais.</p>
            <p>Cordialement,<br>L'équipe LocaGuest</p>";
        return GetBaseTemplate(content);
    }

    private string GetPaymentReminderTemplate(string tenantName, decimal amount, DateTime dueDate, int daysUntilDue)
    {
        var content = $@"
            <h2>Rappel de paiement 🔔</h2>
            <p>Bonjour {tenantName},</p>
            <p>Nous vous rappelons qu'un paiement arrive à échéance dans <strong>{daysUntilDue} jour(s)</strong>.</p>
            <div class='highlight'>
                <p><strong>Montant :</strong> <span class='amount'>{amount:C}</span></p>
                <p><strong>Date d'échéance :</strong> {dueDate:dd/MM/yyyy}</p>
            </div>
            <p>Pensez à effectuer votre paiement avant cette date.</p>
            <p>Cordialement,<br>L'équipe LocaGuest</p>";
        return GetBaseTemplate(content);
    }

    private string GetContractExpiringTemplate(string tenantName, string propertyAddress, DateTime endDate, int daysUntilExpiry)
    {
        var content = $@"
            <h2>Votre bail arrive à échéance 📅</h2>
            <p>Bonjour {tenantName},</p>
            <p>Nous vous informons que votre bail pour le logement situé au <strong>{propertyAddress}</strong> arrive à échéance dans <strong>{daysUntilExpiry} jour(s)</strong>.</p>
            <div class='highlight'>
                <p><strong>Date de fin :</strong> {endDate:dd/MM/yyyy}</p>
            </div>
            <p>Si vous souhaitez renouveler votre bail, merci de nous contacter rapidement.</p>
            <p>Cordialement,<br>L'équipe LocaGuest</p>";
        return GetBaseTemplate(content);
    }

    private string GetContractRenewalTemplate(string tenantName, string propertyAddress, DateTime currentEndDate)
    {
        var content = $@"
            <h2>Proposition de renouvellement 🔄</h2>
            <p>Bonjour {tenantName},</p>
            <p>Votre bail pour le logement situé au <strong>{propertyAddress}</strong> se termine le <strong>{currentEndDate:dd/MM/yyyy}</strong>.</p>
            <p>Nous serions ravis de le renouveler avec vous. Merci de nous faire part de votre décision dans les meilleurs délais.</p>
            <p>Cordialement,<br>L'équipe LocaGuest</p>";
        return GetBaseTemplate(content);
    }

    private string GetMaintenanceScheduledTemplate(string propertyAddress, string description, DateTime scheduledDate)
    {
        var content = $@"
            <h2>Intervention planifiée 🔧</h2>
            <p>Bonjour,</p>
            <p>Une intervention est planifiée pour votre logement au <strong>{propertyAddress}</strong>.</p>
            <div class='highlight'>
                <p><strong>Description :</strong> {description}</p>
                <p><strong>Date prévue :</strong> {scheduledDate:dd/MM/yyyy à HH:mm}</p>
            </div>
            <p>Merci de vous assurer d'être disponible à cette date.</p>
            <p>Cordialement,<br>L'équipe LocaGuest</p>";
        return GetBaseTemplate(content);
    }

    private string GetMaintenanceCompletedTemplate(string propertyAddress, string description, DateTime completedDate)
    {
        var content = $@"
            <h2>Intervention terminée ✅</h2>
            <p>Bonjour,</p>
            <p>L'intervention planifiée pour votre logement au <strong>{propertyAddress}</strong> a été réalisée avec succès.</p>
            <div class='success'>
                <p><strong>Description :</strong> {description}</p>
                <p><strong>Date de réalisation :</strong> {completedDate:dd/MM/yyyy}</p>
            </div>
            <p>Cordialement,<br>L'équipe LocaGuest</p>";
        return GetBaseTemplate(content);
    }

    private string GetDocumentUploadedTemplate(string documentName, string uploadedBy, DateTime uploadedDate)
    {
        var content = $@"
            <h2>Nouveau document disponible 📄</h2>
            <p>Bonjour,</p>
            <p>Un nouveau document a été ajouté à votre espace LocaGuest.</p>
            <div class='highlight'>
                <p><strong>Document :</strong> {documentName}</p>
                <p><strong>Ajouté par :</strong> {uploadedBy}</p>
                <p><strong>Date :</strong> {uploadedDate:dd/MM/yyyy}</p>
            </div>
            <p>Connectez-vous à votre espace pour le consulter.</p>
            <p>Cordialement,<br>L'équipe LocaGuest</p>";
        return GetBaseTemplate(content);
    }

    private string GetWelcomeTemplate(string firstName)
    {
        var content = $@"
            <h2>Bienvenue sur LocaGuest ! 🎉</h2>
            <p>Bonjour {firstName},</p>
            <p>Nous sommes ravis de vous accueillir sur LocaGuest, votre plateforme de gestion locative.</p>
            <div class='highlight'>
                <p><strong>Avec LocaGuest, vous pouvez :</strong></p>
                <ul>
                    <li>Gérer vos biens immobiliers</li>
                    <li>Suivre vos locataires et contrats</li>
                    <li>Gérer les paiements et quittances</li>
                    <li>Suivre la rentabilité de vos investissements</li>
                </ul>
            </div>
            <p>Commencez dès maintenant à profiter de toutes les fonctionnalités !</p>
            <p>Cordialement,<br>L'équipe LocaGuest</p>";
        return GetBaseTemplate(content);
    }

    private string GetPasswordChangedTemplate(string firstName)
    {
        var content = $@"
            <h2>Mot de passe modifié 🔒</h2>
            <p>Bonjour {firstName},</p>
            <p>Nous vous confirmons que votre mot de passe a été modifié avec succès.</p>
            <div class='warning'>
                <p><strong>Si vous n'êtes pas à l'origine de cette modification, contactez-nous immédiatement.</strong></p>
            </div>
            <p>Pour votre sécurité, nous vous recommandons :</p>
            <ul>
                <li>D'utiliser un mot de passe fort et unique</li>
                <li>De ne jamais partager vos identifiants</li>
                <li>De vous déconnecter après utilisation sur un appareil partagé</li>
            </ul>
            <p>Cordialement,<br>L'équipe LocaGuest</p>";
        return GetBaseTemplate(content);
    }

    #endregion
}
