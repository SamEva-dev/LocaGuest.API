using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Organizations.Commands.CreateOrganization;

/// <summary>
/// Command to create a new organization (tenant)
/// Called by AuthGate during registration workflow
/// </summary>
public record CreateOrganizationCommand : IRequest<Result<CreateOrganizationDto>>
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
}

public record CreateOrganizationDto
{
    public Guid OrganizationId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int Number { get; init; }
}
