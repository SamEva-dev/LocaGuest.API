using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.TenantAggregate.Events;

public record OccupantStatusChanged(
    Guid TenantId,
    OccupantStatus NewStatus) : DomainEvent;
