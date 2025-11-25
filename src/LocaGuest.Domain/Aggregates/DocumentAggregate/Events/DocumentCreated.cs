using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.DocumentAggregate.Events;

public record DocumentCreated(
    Guid DocumentId,
    Guid? ContractId,
    Guid? TenantId,
    DocumentType Type,
    DocumentCategory Category) : DomainEvent;
