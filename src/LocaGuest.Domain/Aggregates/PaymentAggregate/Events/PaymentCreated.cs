using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.PaymentAggregate.Events;

public record PaymentCreated(Guid PaymentId, Guid TenantId, decimal Amount) : DomainEvent;
