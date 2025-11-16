using LocaGuest.Application.Common;
using LocaGuest.Application.Features.Organizations.Queries.GetOrganizationById;
using MediatR;

namespace LocaGuest.Application.Features.Organizations.Queries.GetAllOrganizations;

public record GetAllOrganizationsQuery : IRequest<Result<List<OrganizationDto>>>;
