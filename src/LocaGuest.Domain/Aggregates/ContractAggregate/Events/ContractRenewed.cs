using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.ContractAggregate.Events;

public record ContractRenewed(
    Guid ContractId,
    Guid PropertyId,
    Guid TenantId,
    DateTime NewEndDate) : DomainEvent;
