using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.OrganizationAggregate.Events;

public record OrganizationCreated(
    Guid OrganizationId,
    string Code,
    string Name,
    string Email
) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
