using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.TenantAggregate.Events;

public record TenantStatusChanged(
    Guid TenantId,
    TenantStatus NewStatus) : DomainEvent;
