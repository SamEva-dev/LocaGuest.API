using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.OrganizationAggregate.Events;

public record OrganizationDeactivated(
    Guid OrganizationId,
    string Code
) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
