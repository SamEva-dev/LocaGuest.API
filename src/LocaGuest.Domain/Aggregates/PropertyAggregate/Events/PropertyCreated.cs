using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.PropertyAggregate.Events;

public record PropertyCreated(Guid PropertyId, string PropertyName) : DomainEvent;
