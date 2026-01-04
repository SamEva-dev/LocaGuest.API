using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Commands.HardDeleteProvisioningOrganization;

public sealed record HardDeleteProvisioningOrganizationCommand(Guid OrganizationId) : IRequest<Result>;
