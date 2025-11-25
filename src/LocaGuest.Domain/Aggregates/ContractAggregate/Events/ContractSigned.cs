using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.ContractAggregate.Events;

public record ContractSigned(
    Guid ContractId,
    Guid PropertyId,
    Guid TenantId,
    DateTime SignedDate) : DomainEvent;
