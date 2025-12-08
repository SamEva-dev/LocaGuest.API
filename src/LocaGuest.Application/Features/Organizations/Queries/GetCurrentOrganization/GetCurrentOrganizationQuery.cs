using LocaGuest.Application.Common;
using LocaGuest.Application.Features.Organizations.Commands.UpdateOrganizationSettings;
using MediatR;

namespace LocaGuest.Application.Features.Organizations.Queries.GetCurrentOrganization;

/// <summary>
/// Query to get the current user's organization settings
/// </summary>
public record GetCurrentOrganizationQuery : IRequest<Result<OrganizationDto>>
{
    public Guid UserId { get; init; }
}
