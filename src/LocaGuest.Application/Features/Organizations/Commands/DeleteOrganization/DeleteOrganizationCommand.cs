using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Organizations.Commands.DeleteOrganization;

public record DeleteOrganizationCommand(Guid OrganizationId) : IRequest<Result>;
