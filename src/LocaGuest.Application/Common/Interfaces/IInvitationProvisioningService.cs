namespace LocaGuest.Application.Common.Interfaces;

public interface IInvitationProvisioningService
{
    Task<ConsumeInvitationResponse> ConsumeInvitationAsync(
        ConsumeInvitationRequest request,
        string idempotencyKey,
        CancellationToken ct);
}

public sealed record ConsumeInvitationRequest(
    string Token,
    string UserId,
    string UserEmail);

public sealed record ConsumeInvitationResponse(
    Guid OrganizationId,
    Guid TeamMemberId,
    string Role);
