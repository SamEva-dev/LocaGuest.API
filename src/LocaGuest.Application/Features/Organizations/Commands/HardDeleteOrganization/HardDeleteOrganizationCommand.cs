using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Organizations.Commands.HardDeleteOrganization;

/// <summary>
/// Command to permanently delete an organization from the database (hard delete)
/// Use with caution - this cannot be undone!
/// </summary>
public record HardDeleteOrganizationCommand(Guid OrganizationId) : IRequest<Result>;
