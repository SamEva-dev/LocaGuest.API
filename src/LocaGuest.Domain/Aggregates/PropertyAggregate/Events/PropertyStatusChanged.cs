using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.PropertyAggregate.Events;

public record PropertyStatusChanged(
    Guid PropertyId,
    PropertyStatus OldStatus,
    PropertyStatus NewStatus) : DomainEvent;
