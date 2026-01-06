using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.DocumentAggregate.Events;

public record DocumentSigned(
    Guid DocumentId,
    Guid? TenantId,
    DocumentType Type,
    DateTime SignedDate) : DomainEvent;
