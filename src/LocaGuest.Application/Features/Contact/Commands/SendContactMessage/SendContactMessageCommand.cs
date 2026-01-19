using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contact.Commands.SendContactMessage;

public record SendContactMessageCommand(
    string Name,
    string Email,
    string? Subject,
    string Message
) : IRequest<Result<ContactMessageResult>>;

public record ContactMessageResult(
    bool Success,
    string MessageId
);
