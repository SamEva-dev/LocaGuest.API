using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.OccupantAggregate.Events;

public record OccupantDeactivated(Guid OccupantId) : DomainEvent;
