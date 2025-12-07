using LocaGuest.Application.Features.Inventories.Commands.SendInventoryEmail;

namespace LocaGuest.Application.Services;

public interface IEmailService
{
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        List<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default);

    Task SendTeamInvitationEmailAsync(
        string toEmail,
        string invitationToken,
        string organizationName,
        string inviterName,
        string role,
        CancellationToken cancellationToken = default);

    Task SendTeamInvitationAcceptedEmailAsync(
        string toEmail,
        string memberName,
        string organizationName,
        CancellationToken cancellationToken = default);
}
