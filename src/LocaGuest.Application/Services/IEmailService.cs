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
}
