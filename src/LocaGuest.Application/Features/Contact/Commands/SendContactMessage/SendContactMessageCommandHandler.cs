using LocaGuest.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contact.Commands.SendContactMessage;

public class SendContactMessageCommandHandler : IRequestHandler<SendContactMessageCommand, Result<ContactMessageResult>>
{
    private readonly ILogger<SendContactMessageCommandHandler> _logger;

    public SendContactMessageCommandHandler(ILogger<SendContactMessageCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<Result<ContactMessageResult>> Handle(SendContactMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Generate a unique message ID
            var messageId = Guid.NewGuid().ToString("N")[..8].ToUpper();

            _logger.LogInformation(
                "Contact message received - ID: {MessageId}, From: {Name} <{Email}>, Subject: {Subject}",
                messageId,
                request.Name,
                request.Email,
                request.Subject ?? "General"
            );

            // TODO: In production, implement actual email sending here
            // Options:
            // 1. SendGrid: await _sendGridClient.SendEmailAsync(...)
            // 2. SMTP: await _smtpClient.SendMailAsync(...)
            // 3. Azure Communication Services
            // 4. AWS SES

            // For now, we log the message and return success
            // The message could also be stored in a database for later processing

            _logger.LogInformation(
                "Contact message content - ID: {MessageId}\nName: {Name}\nEmail: {Email}\nSubject: {Subject}\nMessage: {Message}",
                messageId,
                request.Name,
                request.Email,
                request.Subject ?? "General",
                request.Message
            );

            // Simulate async operation
            await Task.Delay(100, cancellationToken);

            return Result.Success(new ContactMessageResult(true, messageId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process contact message from {Email}", request.Email);
            return Result.Failure<ContactMessageResult>("Failed to send contact message");
        }
    }
}
