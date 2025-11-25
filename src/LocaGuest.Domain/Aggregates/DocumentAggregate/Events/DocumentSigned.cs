using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.DocumentAggregate.Events;

public record DocumentSigned(
    Guid DocumentId,
    Guid? ContractId,
    Guid? TenantId,
    DocumentType Type,
    DateTime SignedDate) : DomainEvent;
