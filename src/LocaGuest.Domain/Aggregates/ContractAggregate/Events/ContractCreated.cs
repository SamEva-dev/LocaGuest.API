using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.ContractAggregate.Events;

public record ContractCreated(
    Guid ContractId,
    Guid PropertyId,
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate,
    decimal Rent) : DomainEvent;
