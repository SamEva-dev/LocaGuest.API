using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Queries.GetProvisioningOrganizationSessions;

public sealed record GetProvisioningOrganizationSessionsQuery(Guid OrganizationId)
    : IRequest<Result<List<ProvisioningOrganizationSessionDto>>>;

public sealed record ProvisioningOrganizationSessionDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string DeviceName { get; init; } = string.Empty;
    public string Browser { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastActivityAt { get; init; }
}
