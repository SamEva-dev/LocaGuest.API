using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.ContractAggregate.Events;

public record PaymentLateDetected(
    Guid PaymentId,
    Guid ContractId,
    Guid PropertyId,
    Guid TenantId) : DomainEvent;
