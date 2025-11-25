using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.DocumentAggregate.Events;

public record DocumentValidated(
    Guid DocumentId,
    Guid? ContractId,
    DocumentType Type) : DomainEvent;
