using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.PaymentAggregate.Events;

public record PaymentVoided(Guid PaymentId, Guid TenantId) : DomainEvent;
