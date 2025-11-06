using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.ContractAggregate.Events;

public record PaymentRecorded(
    Guid PaymentId,
    Guid ContractId,
    Guid PropertyId,
    Guid TenantId,
    decimal Amount,
    DateTime PaymentDate) : DomainEvent;
