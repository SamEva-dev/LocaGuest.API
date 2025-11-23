using LocaGuest.Domain.Common;

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
    public Guid? AssociatedTenantId { get; private set; }
    
    /// <summary>
    /// Property associated with this document (optional)
    /// </summary>
    public Guid? PropertyId { get; private set; }
    
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
            AssociatedTenantId = tenantId,
            PropertyId = propertyId,
            Description = description,
            ExpiryDate = expiryDate,
            IsArchived = false
        };

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
        AssociatedTenantId = null;
        PropertyId = null;
    }

    public void AssociateToTenant(Guid tenantId)
    {
        AssociatedTenantId = tenantId;
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
    
    /// <summary>Avenant au contrat</summary>
    Avenant,
    
    /// <summary>Autre document</summary>
    Autre
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
    
    /// <summary>Autres documents</summary>
    Autres
}
