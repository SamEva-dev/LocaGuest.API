using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.ContractAggregate;

/// <summary>
/// Avenant au contrat - Document légal modifiant partiellement le bail existant
/// </summary>
public class Addendum : AuditableEntity
{
    public Guid ContractId { get; private set; }
    
    /// <summary>
    /// Type d'avenant
    /// </summary>
    public AddendumType Type { get; private set; }
    
    /// <summary>
    /// Date d'effet de l'avenant
    /// </summary>
    public DateTime EffectiveDate { get; private set; }
    
    /// <summary>
    /// Motif/raison de l'avenant
    /// </summary>
    public string Reason { get; private set; } = string.Empty;
    
    /// <summary>
    /// Description détaillée des modifications
    /// </summary>
    public string Description { get; private set; } = string.Empty;
    
    // ========== MODIFICATIONS FINANCIÈRES ==========
    
    public decimal? OldRent { get; private set; }
    public decimal? NewRent { get; private set; }
    public decimal? OldCharges { get; private set; }
    public decimal? NewCharges { get; private set; }
    
    // ========== MODIFICATIONS DURÉE ==========
    
    public DateTime? OldEndDate { get; private set; }
    public DateTime? NewEndDate { get; private set; }
    
    // ========== MODIFICATIONS OCCUPANTS ==========
    
    /// <summary>
    /// JSON des modifications d'occupants (ajout/retrait)
    /// </summary>
    public string? OccupantChanges { get; private set; }
    
    // ========== MODIFICATIONS CHAMBRE ==========
    
    public Guid? OldRoomId { get; private set; }
    public Guid? NewRoomId { get; private set; }
    
    // ========== MODIFICATIONS CLAUSES ==========
    
    public string? OldClauses { get; private set; }
    public string? NewClauses { get; private set; }
    
    // ========== DOCUMENTS & SIGNATURE ==========
    
    /// <summary>
    /// IDs des documents joints (JSON array)
    /// </summary>
    public string? AttachedDocumentIds { get; private set; }
    
    /// <summary>
    /// Statut de signature
    /// </summary>
    public AddendumSignatureStatus SignatureStatus { get; private set; }
    
    /// <summary>
    /// Date de signature
    /// </summary>
    public DateTime? SignedDate { get; private set; }
    
    /// <summary>
    /// Notes additionnelles
    /// </summary>
    public string? Notes { get; private set; }
    
    // Private constructor for EF
    private Addendum() { }
    
    /// <summary>
    /// Créer un nouvel avenant
    /// </summary>
    public static Addendum Create(
        Guid contractId,
        AddendumType type,
        DateTime effectiveDate,
        string reason,
        string description)
    {
        if (contractId == Guid.Empty)
            throw new ArgumentException("Contract ID cannot be empty", nameof(contractId));
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required", nameof(reason));
        
        if (effectiveDate < DateTime.UtcNow.Date)
            throw new ArgumentException("Effective date cannot be in the past", nameof(effectiveDate));
        
        return new Addendum
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            Type = type,
            EffectiveDate = effectiveDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(effectiveDate, DateTimeKind.Utc) 
                : effectiveDate.ToUniversalTime(),
            Reason = reason,
            Description = description,
            SignatureStatus = AddendumSignatureStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Définir les modifications financières
    /// </summary>
    public void SetFinancialChanges(
        decimal? oldRent, decimal? newRent,
        decimal? oldCharges, decimal? newCharges)
    {
        if (newRent.HasValue && newRent.Value <= 0)
            throw new ArgumentException("New rent must be positive", nameof(newRent));
        
        if (newCharges.HasValue && newCharges.Value < 0)
            throw new ArgumentException("New charges cannot be negative", nameof(newCharges));
        
        OldRent = oldRent;
        NewRent = newRent;
        OldCharges = oldCharges;
        NewCharges = newCharges;
    }
    
    /// <summary>
    /// Définir les modifications de durée
    /// </summary>
    public void SetDurationChanges(DateTime? oldEndDate, DateTime newEndDate)
    {
        if (newEndDate <= DateTime.UtcNow.Date)
            throw new ArgumentException("New end date must be in the future", nameof(newEndDate));
        
        OldEndDate = oldEndDate;
        NewEndDate = newEndDate.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(newEndDate, DateTimeKind.Utc) 
            : newEndDate.ToUniversalTime();
    }
    
    /// <summary>
    /// Définir les modifications de chambre
    /// </summary>
    public void SetRoomChanges(Guid? oldRoomId, Guid newRoomId)
    {
        OldRoomId = oldRoomId;
        NewRoomId = newRoomId;
    }
    
    /// <summary>
    /// Définir les modifications de clauses
    /// </summary>
    public void SetClauseChanges(string? oldClauses, string newClauses)
    {
        if (string.IsNullOrWhiteSpace(newClauses))
            throw new ArgumentException("New clauses cannot be empty", nameof(newClauses));
        
        OldClauses = oldClauses;
        NewClauses = newClauses;
    }
    
    /// <summary>
    /// Définir les modifications d'occupants (JSON)
    /// </summary>
    public void SetOccupantChanges(string occupantChangesJson)
    {
        OccupantChanges = occupantChangesJson;
    }
    
    /// <summary>
    /// Attacher des documents
    /// </summary>
    public void AttachDocuments(List<Guid> documentIds)
    {
        if (documentIds != null && documentIds.Any())
        {
            AttachedDocumentIds = System.Text.Json.JsonSerializer.Serialize(documentIds);
        }
    }
    
    /// <summary>
    /// Ajouter des notes
    /// </summary>
    public void AddNotes(string notes)
    {
        Notes = notes;
    }

    public void UpdateDetails(DateTime effectiveDate, string reason, string description)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required", nameof(reason));

        if (effectiveDate < DateTime.UtcNow.Date)
            throw new ArgumentException("Effective date cannot be in the past", nameof(effectiveDate));

        EffectiveDate = effectiveDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(effectiveDate, DateTimeKind.Utc)
            : effectiveDate.ToUniversalTime();

        Reason = reason;
        Description = description;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void UpdateDocuments(List<Guid> documentIds)
    {
        AttachedDocumentIds = documentIds != null && documentIds.Any()
            ? System.Text.Json.JsonSerializer.Serialize(documentIds)
            : null;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Marquer comme signé
    /// </summary>
    public void MarkAsSigned(DateTime? signedDate = null)
    {
        SignatureStatus = AddendumSignatureStatus.Signed;
        SignedDate = signedDate ?? DateTime.UtcNow;
    }
    
    /// <summary>
    /// Rejeter la signature
    /// </summary>
    public void Reject()
    {
        SignatureStatus = AddendumSignatureStatus.Rejected;
    }
}

/// <summary>
/// Type d'avenant
/// </summary>
public enum AddendumType
{
    Financial = 1,      // Modification loyer/charges
    Duration = 2,       // Modification dates
    Occupants = 3,      // Ajout/retrait colocataires
    Clauses = 4,        // Modification clauses
    Free = 5            // Avenant libre
}

/// <summary>
/// Statut de signature de l'avenant
/// </summary>
public enum AddendumSignatureStatus
{
    Pending = 1,        // En attente de signature
    Signed = 2,         // Signé
    Rejected = 3        // Refusé
}
