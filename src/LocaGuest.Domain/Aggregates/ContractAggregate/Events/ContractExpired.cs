using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.ContractAggregate.Events;

public record ContractExpired(
    Guid ContractId,
    Guid PropertyId,
    Guid TenantId,
    DateTime EndDate) : DomainEvent;
