namespace LocaGuest.Application.Common.Interfaces;

public interface IProvisioningService
{
    Task<ProvisionOrganizationResponse> ProvisionOrganizationAsync(
        ProvisionOrganizationRequest request,
        string idempotencyKey,
        CancellationToken ct);
}

public sealed record ProvisionOrganizationRequest(
    string OrganizationName,
    string OrganizationEmail,
    string? OrganizationPhone,
    string OwnerUserId,
    string OwnerEmail
);

public sealed record ProvisionOrganizationResponse(
    Guid OrganizationId,
    int Number,
    string Code,
    string Name,
    string Email
);
