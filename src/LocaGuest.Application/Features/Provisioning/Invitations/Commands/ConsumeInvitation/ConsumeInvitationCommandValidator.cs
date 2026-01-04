using FluentValidation;

namespace LocaGuest.Application.Features.Provisioning.Invitations.Commands.ConsumeInvitation;

public sealed class ConsumeInvitationCommandValidator : AbstractValidator<ConsumeInvitationCommand>
{
    public ConsumeInvitationCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required")
            .Must(id => Guid.TryParse(id, out var g) && g != Guid.Empty)
            .WithMessage("UserId must be a valid GUID");

        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("UserEmail is required")
            .EmailAddress().WithMessage("UserEmail must be a valid email");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required");
    }
}
