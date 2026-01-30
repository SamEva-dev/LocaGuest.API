using MediatR;

namespace LocaGuest.Api.Features.TenantOnboarding.GetInvitation;

public sealed record GetTenantOnboardingInvitationQuery(string Token)
    : IRequest<GetTenantOnboardingInvitationResult>;

public sealed record GetTenantOnboardingInvitationResult(
    bool IsValid,
    string? Message,
    GetTenantOnboardingInvitationResponse? Invitation);

public sealed record GetTenantOnboardingInvitationResponse(
    string Email,
    Guid? PropertyId,
    DateTime ExpiresAtUtc);
