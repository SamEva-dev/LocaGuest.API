using LocaGuest.Domain.Common;
using LocaGuest.Domain.Aggregates.DocumentAggregate.Events;
using LocaGuest.Domain.Exceptions;

namespace LocaGuest.Domain.Aggregates.DocumentAggregate;

public class Document : AuditableEntity
{
    /// <summary>
    /// Auto-generated unique code (e.g., T0001-DOC0001)
    /// </summary>
    public string Code { get; private set; } = string.Empty;
    
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public DocumentType Type { get; private set; }
    public DocumentCategory Category { get; private set; }
    public long FileSizeBytes { get; private set; }
    public string? Description { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    
    /// <summary>
    /// Locataire associé à ce document (optionnel)
    /// Note: Le TenantId hérité de AuditableEntity est utilisé pour le multi-tenant filtering
    /// </summary>
    public Guid? AssociatedOccupantId { get; private set; }
    
    /// <summary>
    /// Property associated with this document (optional)
    /// </summary>
    public Guid? PropertyId { get; private set; }
    
    /// <summary>
    /// Statut du document (Draft, Signed, Validated, Archived)
    /// </summary>
    public DocumentStatus Status { get; private set; }
    
    /// <summary>
    /// Date de signature du document
    /// </summary>
    public DateTime? SignedDate { get; private set; }
    
    /// <summary>
    /// Nom de la personne qui a signé le document
    /// </summary>
    public string? SignedBy { get; private set; }
    
    /// <summary>
    /// If true, the document is still in storage but dissociated from tenant/property
    /// </summary>
    public bool IsArchived { get; private set; }

    private Document() { } // EF

    public static Document Create(
        string fileName,
        string filePath,
        DocumentType type,
        DocumentCategory category,
        long fileSizeBytes,
        Guid? tenantId = null,
        Guid? propertyId = null,
        string? description = null,
        DateTime? expiryDate = null)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            FilePath = filePath,
            Type = type,
            Category = category,
            FileSizeBytes = fileSizeBytes,
            AssociatedOccupantId = tenantId,
            PropertyId = propertyId,
            Description = description,
            ExpiryDate = expiryDate,
            Status = DocumentStatus.Draft, // Toujours créé en Draft
            IsArchived = false
        };

        document.AddDomainEvent(new DocumentCreated(
            document.Id,
            tenantId,
            type,
            category));

        return document;
    }

    public void SetCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(Code))
            throw new InvalidOperationException("Code cannot be changed once set");
        
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));
        
        Code = code;
    }

    public void Archive()
    {
        IsArchived = true;
        AssociatedOccupantId = null;
        PropertyId = null;
    }

    public void AssociateToTenant(Guid tenantId)
    {
        AssociatedOccupantId = tenantId;
        IsArchived = false;
    }

    public void AssociateToProperty(Guid propertyId)
    {
        PropertyId = propertyId;
        IsArchived = false;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
    }

    public void UpdateExpiryDate(DateTime? expiryDate)
    {
        ExpiryDate = expiryDate;
    }
    
    /// <summary>
    /// Marquer le document comme signé
    /// Transition: Draft → Signed
    /// </summary>
    public void MarkAsSigned(DateTime? signedDate = null, string? signedBy = null)
    {
        if (Status != DocumentStatus.Draft)
            throw new ValidationException("DOCUMENT_ALREADY_SIGNED", 
                "Only draft documents can be marked as signed");
        
        Status = DocumentStatus.Signed;
        SignedDate = signedDate ?? DateTime.UtcNow;
        SignedBy = signedBy;
        
        AddDomainEvent(new DocumentSigned(
            Id,
            AssociatedOccupantId,
            Type,
            SignedDate.Value));
    }
    
    /// <summary>
    /// Valider le document (par le bailleur)
    /// Transition: Signed → Validated
    /// </summary>
    public void Validate()
    {
        if (Status != DocumentStatus.Signed)
            throw new ValidationException("DOCUMENT_NOT_SIGNED", 
                "Only signed documents can be validated");
        
        Status = DocumentStatus.Validated;
        AddDomainEvent(new DocumentValidated(Id, Type));
    }
}

/// <summary>
/// Type de document
/// </summary>
public enum DocumentType
{
    /// <summary>Contrat de bail (location seule)</summary>
    Bail,
    
    /// <summary>Contrat de colocation</summary>
    Colocation,
    
    /// <summary>État des lieux d'entrée</summary>
    EtatDesLieuxEntree,
    
    /// <summary>État des lieux de sortie</summary>
    EtatDesLieuxSortie,
    
    /// <summary>Pièce d'identité (CNI, passeport, etc.)</summary>
    PieceIdentite,
    
    /// <summary>Attestation d'assurance habitation</summary>
    Assurance,
    
    /// <summary>Justificatif de domicile</summary>
    JustificatifDomicile,
    
    /// <summary>Bulletin de salaire</summary>
    BulletinSalaire,
    
    /// <summary>Avis d'imposition</summary>
    AvisImposition,
    
    /// <summary>Quittance de loyer</summary>
    Quittance,

    /// <summary>Facture de loyer</summary>
    Facture,
    
    /// <summary>Avenant au contrat</summary>
    Avenant,
    
    /// <summary>Autre document</summary>
    Autre
}

/// <summary>
/// Statut d'un document
/// </summary>
public enum DocumentStatus
{
    /// <summary>Brouillon, non signé</summary>
    Draft,
    
    /// <summary>Signé par le locataire</summary>
    Signed,
    
    /// <summary>Validé par le bailleur</summary>
    Validated,
    
    /// <summary>Archivé</summary>
    Archived
}

/// <summary>
/// Catégorie de document pour l'organisation
/// </summary>
public enum DocumentCategory
{
    /// <summary>Contrats (bail, colocation, avenant)</summary>
    Contrats,
    
    /// <summary>États des lieux</summary>
    EtatsDesLieux,
    
    /// <summary>Identité du locataire</summary>
    Identite,
    
    /// <summary>Justificatifs divers</summary>
    Justificatifs,
    
    /// <summary>Quittances de loyer</summary>
    Quittances,

    /// <summary>Factures de loyer</summary>
    Factures,
    
    /// <summary>Autres documents</summary>
    Autres
}
