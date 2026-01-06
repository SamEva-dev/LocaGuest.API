using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.DocumentAggregate.Events;

public record DocumentValidated(
    Guid DocumentId,
    DocumentType Type) : DomainEvent;
