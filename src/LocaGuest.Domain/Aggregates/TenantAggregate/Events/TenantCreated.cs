using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.TenantAggregate.Events;

public record TenantCreated(Guid TenantId, string FullName, string Email) : DomainEvent;
