using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.PaymentAggregate.Events;

public record PaymentUpdated(Guid PaymentId, Guid TenantId, decimal NewAmount) : DomainEvent;
