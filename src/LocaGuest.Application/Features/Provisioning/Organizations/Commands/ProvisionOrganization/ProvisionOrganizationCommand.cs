using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Commands.ProvisionOrganization;

public sealed record ProvisionOrganizationCommand(
    string OrganizationName,
    string OrganizationEmail,
    string? OrganizationPhone,
    string OwnerUserId,
    string OwnerEmail,
    string IdempotencyKey) : IRequest<Result<ProvisionOrganizationResponseDto>>;

public sealed record ProvisionOrganizationResponseDto(
    Guid OrganizationId,
    int Number,
    string Code,
    string Name,
    string Email);
