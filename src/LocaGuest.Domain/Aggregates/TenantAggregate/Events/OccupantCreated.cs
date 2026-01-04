using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.TenantAggregate.Events;

public record OccupantCreated(Guid TenantId, string FullName, string Email) : DomainEvent;
