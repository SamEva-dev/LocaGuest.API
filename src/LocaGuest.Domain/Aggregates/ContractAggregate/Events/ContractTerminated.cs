using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.ContractAggregate.Events;

public record ContractTerminated(
    Guid ContractId,
    Guid PropertyId,
    Guid TenantId,
    DateTime TerminationDate) : DomainEvent;
