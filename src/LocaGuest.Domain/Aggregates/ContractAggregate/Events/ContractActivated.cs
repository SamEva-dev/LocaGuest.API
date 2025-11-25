using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.ContractAggregate.Events;

public record ContractActivated(
    Guid ContractId,
    Guid PropertyId,
    Guid TenantId) : DomainEvent;
