using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.OccupantAggregate.Events;

public record OccupantStatusChanged(
    Guid OccupantId,
    OccupantStatus NewStatus) : DomainEvent;
