using LocaGuest.Application.Common;
using LocaGuest.Application.Features.Organizations.Queries.GetOrganizationById;
using MediatR;

namespace LocaGuest.Application.Features.Organizations.Queries.GetActiveOrganizations;

/// <summary>
/// Query to get all active organizations (excluding inactive/deleted ones)
/// </summary>
public record GetActiveOrganizationsQuery : IRequest<Result<List<OrganizationDto>>>;
