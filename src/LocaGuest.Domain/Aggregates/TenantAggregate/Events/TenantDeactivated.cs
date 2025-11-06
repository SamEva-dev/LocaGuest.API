using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.TenantAggregate.Events;

public record TenantDeactivated(Guid TenantId) : DomainEvent;
