using MediatR;

namespace LocaGuest.Api.Features.TenantOnboarding.CreateInvitation;

public sealed record CreateTenantOnboardingInvitationCommand(
    Guid OrganizationId,
    string Email,
    Guid? PropertyId) : IRequest<CreateTenantOnboardingInvitationResponse>;

public sealed record CreateTenantOnboardingInvitationResponse(
    string Token,
    string Link,
    DateTime ExpiresAtUtc);
