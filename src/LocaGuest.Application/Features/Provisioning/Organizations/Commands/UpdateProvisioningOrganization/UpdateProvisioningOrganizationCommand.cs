using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Commands.UpdateProvisioningOrganization;

public sealed record UpdateProvisioningOrganizationCommand(
    Guid OrganizationId,
    string? Name,
    string? Email,
    string? Phone,
    string? Status) : IRequest<Result<UpdateProvisioningOrganizationDto>>;

public sealed record UpdateProvisioningOrganizationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string Status { get; init; } = string.Empty;
}
