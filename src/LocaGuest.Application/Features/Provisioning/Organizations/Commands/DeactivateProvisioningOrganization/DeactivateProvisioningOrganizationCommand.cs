using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Commands.DeactivateProvisioningOrganization;

public sealed record DeactivateProvisioningOrganizationCommand(Guid OrganizationId) : IRequest<Result>;
