using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.ContractAggregate.Events;

public record ContractPending(
    Guid ContractId,
    Guid PropertyId,
    Guid TenantId) : DomainEvent;
