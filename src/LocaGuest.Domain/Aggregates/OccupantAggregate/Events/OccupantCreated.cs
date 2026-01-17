using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.OccupantAggregate.Events;

public record OccupantCreated(Guid OccupantId, string FullName, string Email) : DomainEvent;
