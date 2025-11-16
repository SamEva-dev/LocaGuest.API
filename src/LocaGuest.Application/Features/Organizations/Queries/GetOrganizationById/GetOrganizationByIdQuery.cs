using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Organizations.Queries.GetOrganizationById;

public record GetOrganizationByIdQuery(Guid OrganizationId) : IRequest<Result<OrganizationDto>>;

public record OrganizationDto
{
    public Guid Id { get; init; }
    public int Number { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? SubscriptionPlan { get; init; }
    public DateTime? SubscriptionExpiryDate { get; init; }
}
