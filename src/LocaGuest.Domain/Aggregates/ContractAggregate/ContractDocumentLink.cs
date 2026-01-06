using LocaGuest.Domain.Aggregates.DocumentAggregate;

namespace LocaGuest.Domain.Aggregates.ContractAggregate;

public class ContractDocumentLink
{
    public Guid OrganizationId { get; private set; }
    public Guid ContractId { get; private set; }
    public Guid DocumentId { get; private set; }

    public DocumentType Type { get; private set; }
    public DateTime LinkedAtUtc { get; private set; }

    private ContractDocumentLink() { }

    public static ContractDocumentLink Create(Guid organizationId, Guid contractId, Guid documentId, DocumentType type)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId cannot be empty.", nameof(organizationId));

        if (contractId == Guid.Empty)
            throw new ArgumentException("ContractId cannot be empty.", nameof(contractId));

        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId cannot be empty.", nameof(documentId));

        return new ContractDocumentLink
        {
            OrganizationId = organizationId,
            ContractId = contractId,
            DocumentId = documentId,
            Type = type,
            LinkedAtUtc = DateTime.UtcNow
        };
    }
}
