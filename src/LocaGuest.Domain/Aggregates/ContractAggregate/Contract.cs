using LocaGuest.Domain.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate.Events;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Exceptions;

namespace LocaGuest.Domain.Aggregates.ContractAggregate;

public class Contract : AuditableEntity
{
    /// <summary>
    /// Auto-generated unique code (e.g., T0001-CTR0001)
    /// Format: {TenantCode}-CTR{Number}
    /// </summary>
    public string Code { get; private set; } = string.Empty;
    
    public Guid PropertyId { get; private set; }
    
    /// <summary>
    /// ID du locataire (Tenant entity) - ne pas confondre avec TenantId multi-tenant hérité de AuditableEntity
    /// </summary>
    public Guid RenterTenantId { get; private set; }
    
    public ContractType Type { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal Rent { get; private set; }
    public decimal Charges { get; private set; }
    public decimal? Deposit { get; private set; }
    
    /// <summary>
    /// Jour du mois limite pour le paiement (1-31). Par défaut: 5
    /// Utilisé pour calculer les retards de paiement
    /// </summary>
    public int PaymentDueDay { get; private set; } = 5;
    
    public ContractStatus Status { get; private set; }
    public string? Notes { get; private set; }

    public DateTime? TerminationDate { get; private set; }
    public string? TerminationReason { get; private set; }

    public DateTime? NoticeDate { get; private set; }
    public DateTime? NoticeEndDate { get; private set; }
    public string? NoticeReason { get; private set; }
    
    /// <summary>
    /// Identifiant de la chambre pour les colocations individuelles
    /// Null pour location complète ou colocation solidaire
    /// </summary>
    public Guid? RoomId { get; private set; }
    
    /// <summary>
    /// Marque le contrat comme en conflit (autre contrat signé sur même bien/chambre)
    /// </summary>
    public bool IsConflict { get; private set; }
    
    /// <summary>
    /// ID du contrat de renouvellement (si ce contrat a été renouvelé)
    /// </summary>
    public Guid? RenewedContractId { get; private set; }
    
    /// <summary>
    /// Clauses personnalisées du contrat (spécificités, interdictions, etc.)
    /// </summary>
    public string? CustomClauses { get; private set; }
    
    /// <summary>
    /// Ancien IRL utilisé pour la révision (Indice de Référence des Loyers)
    /// </summary>
    public decimal? PreviousIRL { get; private set; }
    
    /// <summary>
    /// Nouvel IRL utilisé pour la révision
    /// </summary>
    public decimal? CurrentIRL { get; private set; }

    private readonly List<ContractPayment> _payments = new();
    public IReadOnlyCollection<ContractPayment> Payments => _payments.AsReadOnly();
    
    private readonly List<RequiredDocument> _requiredDocuments = new();
    public IReadOnlyCollection<RequiredDocument> RequiredDocuments => _requiredDocuments.AsReadOnly();
    
    private readonly List<Guid> _documentIds = new();
    public IReadOnlyCollection<Guid> DocumentIds => _documentIds.AsReadOnly();
    
    private readonly List<Addendum> _addendums = new();
    public IReadOnlyCollection<Addendum> Addendums => _addendums.AsReadOnly();

    private Contract() { } // EF

    public static Contract Create(
        Guid propertyId,
        Guid renterTenantId,
        ContractType type,
        DateTime startDate,
        DateTime endDate,
        decimal rent,
        decimal charges = 0,
        decimal? deposit = null,
        int paymentDueDay = 5,
        Guid? roomId = null)
    {
        // Validations dates
        if (startDate >= endDate)
            throw new ValidationException("CONTRACT_INVALID_DATES", "Start date must be before end date");

        // Validations montants
        if (rent <= 0)
            throw new ValidationException("CONTRACT_INVALID_RENT", "Rent must be positive");
        
        if (charges < 0)
            throw new ValidationException("CONTRACT_INVALID_CHARGES", "Charges cannot be negative");

        // Validation PaymentDueDay
        if (paymentDueDay < 1 || paymentDueDay > 31)
            throw new ValidationException("CONTRACT_INVALID_PAYMENT_DUE_DAY", "Payment due day must be between 1 and 31");

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            RenterTenantId = renterTenantId,
            Type = type,
            StartDate = startDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(startDate, DateTimeKind.Utc) : startDate.ToUniversalTime(),
            EndDate = endDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(endDate, DateTimeKind.Utc) : endDate.ToUniversalTime(),
            Rent = rent,
            Charges = charges,
            Deposit = deposit,
            PaymentDueDay = paymentDueDay,
            RoomId = roomId,
            Status = ContractStatus.Draft,
            IsConflict = false
        };

        // Définir les documents requis selon le type de contrat
        contract.InitializeRequiredDocuments(type);
        
        contract.AddDomainEvent(new ContractCreated(contract.Id, propertyId, renterTenantId, startDate, endDate, rent));
        return contract;
    }

    /// <summary>
    /// Set the auto-generated code (called once after creation)
    /// Code is immutable after being set
    /// </summary>
    public void SetCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(Code))
            throw new InvalidOperationException("Code cannot be changed once set");
        
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));
        
        Code = code;
    }
    
    /// <summary>
    /// Mettre à jour les informations de base d'un contrat Draft
    /// </summary>
    public void UpdateBasicInfo(
        Guid renterTenantId,
        Guid propertyId,
        Guid? roomId,
        string type,
        DateTime startDate,
        DateTime endDate,
        decimal rent,
        decimal charges,
        decimal? deposit)
    {
        if (Status != ContractStatus.Draft)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only draft contracts can be updated");
        
        if (startDate >= endDate)
            throw new ValidationException("CONTRACT_INVALID_DATES", "Start date must be before end date");
        
        if (rent <= 0)
            throw new ValidationException("CONTRACT_INVALID_RENT", "Rent must be positive");
        
        if (charges < 0)
            throw new ValidationException("CONTRACT_INVALID_CHARGES", "Charges cannot be negative");

        if (deposit is < 0)
            throw new ValidationException("CONTRACT_INVALID_DEPOSIT", "Deposit cannot be negative");
        
        RenterTenantId = renterTenantId;
        PropertyId = propertyId;
        RoomId = roomId;
        Type = Enum.Parse<ContractType>(type, ignoreCase: true);
        StartDate = startDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(startDate, DateTimeKind.Utc) : startDate.ToUniversalTime();
        EndDate = endDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(endDate, DateTimeKind.Utc) : endDate.ToUniversalTime();
        Rent = rent;
        Charges = charges;
        Deposit = deposit;
    }

    public void Renew(DateTime newEndDate)
    {
        if (newEndDate <= EndDate)
            throw new ValidationException("CONTRACT_INVALID_RENEWAL", "New end date must be after current end date");

        EndDate = newEndDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(newEndDate, DateTimeKind.Utc) : newEndDate.ToUniversalTime();
        AddDomainEvent(new ContractRenewed(Id, PropertyId, RenterTenantId, EndDate));
    }

    public void Terminate(DateTime terminationDate, string? reason = null)
    {
        if (Status == ContractStatus.Terminated) return;

        Status = ContractStatus.Terminated;
        TerminationDate = terminationDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(terminationDate, DateTimeKind.Utc)
            : terminationDate.ToUniversalTime();
        TerminationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        AddDomainEvent(new ContractTerminated(Id, PropertyId, RenterTenantId, terminationDate));
    }

    public void GiveNotice(DateTime noticeDate, DateTime noticeEndDate, string reason)
    {
        if (Status != ContractStatus.Active && Status != ContractStatus.Signed)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only Active or Signed contracts can receive a notice");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("NOTICE_REASON_REQUIRED", "Reason is required");

        var nDate = noticeDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(noticeDate, DateTimeKind.Utc)
            : noticeDate.ToUniversalTime();

        var nEnd = noticeEndDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(noticeEndDate, DateTimeKind.Utc)
            : noticeEndDate.ToUniversalTime();

        if (nEnd <= nDate)
            throw new ValidationException("NOTICE_INVALID_DATES", "Notice end date must be after notice date");

        NoticeDate = nDate;
        NoticeEndDate = nEnd;
        NoticeReason = reason.Trim();
    }

    public void CancelNotice()
    {
        NoticeDate = null;
        NoticeEndDate = null;
        NoticeReason = null;
    }

    /// <summary>
    /// Annuler un contrat non signé (Draft ou Pending)
    /// Utilisé quand un autre contrat est choisi comme contrat principal
    /// </summary>
    public void Cancel()
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.Pending)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only draft or pending contracts can be cancelled");

        Status = ContractStatus.Cancelled;
        AddDomainEvent(new ContractCancelled(Id, PropertyId, RenterTenantId));
    }

    public void CancelSigned()
    {
        if (Status != ContractStatus.Signed)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only signed contracts can be cancelled");

        Status = ContractStatus.Cancelled;
        AddDomainEvent(new ContractCancelled(Id, PropertyId, RenterTenantId));
    }
    
    /// <summary>
    /// Marquer le contrat comme en conflit (autre contrat signé sur même bien/chambre)
    /// </summary>
    public void MarkAsConflict()
    {
        if (Status != ContractStatus.Draft)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only draft contracts can be marked as conflict");
            
        IsConflict = true;
        Status = ContractStatus.Cancelled;
        AddDomainEvent(new ContractCancelled(Id, PropertyId, RenterTenantId));
    }

    public void MarkAsExpiring()
    {
        if (Status == ContractStatus.Active)
        {
            Status = ContractStatus.Expiring;
        }
    }
    
    /// <summary>
    /// Marquer le contrat comme expiré (arrivé à terme)
    /// Transition: Active ou Expiring → Expired
    /// DÉCLENCHE: Locataire Inactive + Bien Vacant (si pas d'autre contrat)
    /// </summary>
    public void MarkAsExpired()
    {
        if (Status != ContractStatus.Active && Status != ContractStatus.Expiring)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only active or expiring contracts can be marked as expired");
            
        Status = ContractStatus.Expired;
        AddDomainEvent(new ContractExpired(Id, PropertyId, RenterTenantId, EndDate));
    }
    
    /// <summary>
    /// Marquer le contrat comme renouvelé (remplacé par un nouveau contrat)
    /// Transition: Active ou Expiring → Renewed
    /// DÉCLENCHE: Création d'un nouveau contrat qui prend le relais
    /// </summary>
    public void MarkAsRenewed(Guid newContractId)
    {
        if (Status != ContractStatus.Active && Status != ContractStatus.Expiring)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only active or expiring contracts can be renewed");
            
        RenewedContractId = newContractId;
        Status = ContractStatus.Renewed;
        AddDomainEvent(new ContractRenewed(Id, PropertyId, RenterTenantId, EndDate));
    }
    
    /// <summary>
    /// Mettre à jour les clauses personnalisées
    /// </summary>
    public void UpdateCustomClauses(string? clauses)
    {
        CustomClauses = clauses;
    }
    
    /// <summary>
    /// Mettre à jour les indices IRL pour la révision de loyer
    /// </summary>
    public void UpdateIRL(decimal? previousIRL, decimal? currentIRL)
    {
        PreviousIRL = previousIRL;
        CurrentIRL = currentIRL;
    }

    /// <summary>
    /// Marquer le contrat comme en attente de signature (PDF généré)
    /// Transition: Draft → Pending
    /// </summary>
    public void MarkAsPending()
    {
        if (Status != ContractStatus.Draft)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only draft contracts can be marked as pending");
            
        Status = ContractStatus.Pending;
        AddDomainEvent(new ContractPending(Id, PropertyId, RenterTenantId));
    }
    
    /// <summary>
    /// Marquer le contrat comme signé (bail signé juridiquement)
    /// Transition: Draft ou Pending → Signed
    /// DÉCLENCHE: Locataire Reserved + Bien Reserved
    /// </summary>
    public void MarkAsSigned(DateTime? signedDate = null)
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.Pending)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only draft or pending contracts can be marked as signed");

        Status = ContractStatus.Signed;
        var effectiveSignedDate = signedDate ?? DateTime.UtcNow;
        AddDomainEvent(new ContractSigned(Id, PropertyId, RenterTenantId, effectiveSignedDate));
    }

    /// <summary>
    /// Activer un contrat signé (au jour de la date de début)
    /// Transition: Signed → Active
    /// DÉCLENCHE: Locataire Occupant + Bien Occupé
    /// </summary>
    public void Activate()
    {
        if (Status != ContractStatus.Signed)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only signed contracts can be activated");

        if (DateTime.UtcNow < StartDate)
            throw new ValidationException("CONTRACT_NOT_STARTED", "Contract cannot be activated before start date");

        Status = ContractStatus.Active;
        AddDomainEvent(new ContractActivated(Id, PropertyId, RenterTenantId));
    }

    public ContractPayment RecordPayment(decimal amount, DateTime paymentDate, ContractPaymentMethod method)
    {
        if (amount <= 0)
            throw new ValidationException("PAYMENT_INVALID_AMOUNT", "Payment amount must be positive");

        var payment = ContractPayment.Create(Id, amount, paymentDate, method);
        _payments.Add(payment);

        AddDomainEvent(new PaymentRecorded(payment.Id, Id, PropertyId, RenterTenantId, amount, paymentDate));
        return payment;
    }

    public void MarkPaymentAsLate(Guid paymentId)
    {
        var payment = _payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment == null)
            throw new NotFoundException("PAYMENT_NOT_FOUND", "Payment not found");

        payment.MarkAsLate();
        AddDomainEvent(new PaymentLateDetected(paymentId, Id, PropertyId, RenterTenantId));
    }
    
    /// <summary>
    /// Initialiser les documents requis selon le type de contrat
    /// Un contrat est valide quand tous les documents REQUIS sont signés
    /// Pour un contrat de location (Furnished/Unfurnished):
    ///   - Bail signé (requis)
    ///   - État des lieux d'entrée signé (requis)
    ///   - Attestation d'assurance (optionnel)
    /// </summary>
    private void InitializeRequiredDocuments(ContractType type)
    {
        _requiredDocuments.Clear();
        
        // Pour tous les types de contrats (Furnished ou Unfurnished)
        // Documents REQUIS pour valider l'occupation du logement
        _requiredDocuments.Add(new RequiredDocument(DocumentType.Bail, isRequired: true));
        _requiredDocuments.Add(new RequiredDocument(DocumentType.EtatDesLieuxEntree, isRequired: true));
        
        // Document OPTIONNEL mais recommandé
        _requiredDocuments.Add(new RequiredDocument(DocumentType.Assurance, isRequired: false));
        
        // Note: Si besoin de gérer des Avenants, il faudra ajouter un ContractType.Amendment
        // qui ne nécessiterait que le document Avenant signé
    }
    
    /// <summary>
    /// Associer un document à ce contrat
    /// </summary>
    public void AssociateDocument(Guid documentId, DocumentType type)
    {
        if (!_documentIds.Contains(documentId))
        {
            _documentIds.Add(documentId);
            
            var required = _requiredDocuments.FirstOrDefault(r => r.Type == type);
            if (required != null)
            {
                required.MarkAsProvided();
            }
        }
    }
    
    /// <summary>
    /// Notifier qu'un document a été signé
    /// Vérifie si tous les documents requis sont signés pour marquer comme Signed
    /// </summary>
    public void OnDocumentSigned(DocumentType type)
    {
        var required = _requiredDocuments.FirstOrDefault(r => r.Type == type);
        if (required != null)
        {
            required.MarkAsSigned();
        }
        
        // Vérifier si tous les documents requis sont signés
        if (AreAllRequiredDocumentsSigned())
        {
            if (Status == ContractStatus.Draft || Status == ContractStatus.Pending)
            {
                // Tous documents signés → Marquer comme Signed
                MarkAsSigned();
            }
        }
        else if (Status == ContractStatus.Draft)
        {
            // Au moins un document signé mais pas tous → Passer en Pending
            MarkAsPending();
        }
    }
    
    /// <summary>
    /// Vérifier si tous les documents requis sont signés
    /// </summary>
    public bool AreAllRequiredDocumentsSigned()
    {
        return _requiredDocuments
            .Where(r => r.IsRequired)
            .All(r => r.IsSigned);
    }
    
    /// <summary>
    /// Vérifier si le contrat peut être activé (au jour de début)
    /// </summary>
    public bool CanActivate()
    {
        return Status == ContractStatus.Signed 
            && DateTime.UtcNow >= StartDate;
    }
    
    // ========== GESTION DES AVENANTS ==========
    
    /// <summary>
    /// Ajouter un avenant au contrat
    /// </summary>
    public void AddAddendum(Addendum addendum)
    {
        if (addendum == null)
            throw new ArgumentNullException(nameof(addendum));
        
        _addendums.Add(addendum);
    }
    
    /// <summary>
    /// Vérifier si un avenant peut être créé
    /// </summary>
    public bool CanCreateAddendum()
    {
        // Un avenant ne peut être créé que si :
        // - Le contrat est Active ou Signed
        // - Pas de procédure de sortie (Terminated)
        // - Pas de renouvellement (Renewed)
        return (Status == ContractStatus.Active || Status == ContractStatus.Signed) 
            && Status != ContractStatus.Terminated
            && Status != ContractStatus.Renewed
            && Status != ContractStatus.Expired;
    }
    
    /// <summary>
    /// Appliquer les modifications d'un avenant financier au contrat
    /// </summary>
    public void ApplyFinancialAddendum(decimal newRent, decimal newCharges)
    {
        if (newRent <= 0)
            throw new ValidationException("INVALID_RENT", "Rent must be positive");
        
        if (newCharges < 0)
            throw new ValidationException("INVALID_CHARGES", "Charges cannot be negative");
        
        Rent = newRent;
        Charges = newCharges;
    }
    
    /// <summary>
    /// Appliquer les modifications de durée au contrat
    /// </summary>
    public void ApplyDurationAddendum(DateTime newEndDate)
    {
        if (newEndDate <= DateTime.UtcNow)
            throw new ValidationException("INVALID_DATE", "New end date must be in the future");
        
        if (newEndDate <= StartDate)
            throw new ValidationException("INVALID_DATE", "End date must be after start date");
        
        EndDate = newEndDate.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(newEndDate, DateTimeKind.Utc) 
            : newEndDate.ToUniversalTime();
    }
    
    /// <summary>
    /// Appliquer les modifications de chambre au contrat (colocation)
    /// </summary>
    public void ApplyRoomAddendum(Guid newRoomId)
    {
        RoomId = newRoomId;
    }
    
    /// <summary>
    /// Appliquer les modifications de clauses au contrat
    /// </summary>
    public void ApplyClausesAddendum(string newClauses)
    {
        if (string.IsNullOrWhiteSpace(newClauses))
            throw new ValidationException("INVALID_CLAUSES", "Clauses cannot be empty");
        
        CustomClauses = newClauses;
    }
}

public enum ContractType
{
    Furnished,
    Unfurnished
}

public enum ContractStatus
{
    Draft,          // Brouillon modifiable, aucun impact réel
    Pending,        // En attente de signature (PDF généré mais pas signé)
    Signed,         // Bail signé et validé juridiquement (déclenche réservation)
    Active,         // Bail commence le jour de la date de début (locataire occupant)
    Expiring,       // Expire bientôt (notification automatique)
    Terminated,     // Rupture anticipée ou fin de bail
    Expired,        // Bail arrivé à terme
    Cancelled,      // Annulé (contrat Draft qui n'a pas été choisi, conflit)
    Renewed         // Contrat renouvelé (remplacé par un nouveau contrat)
}

/// <summary>
/// Représente un document requis pour un contrat
/// </summary>
public class RequiredDocument
{
    public DocumentType Type { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsProvided { get; private set; }
    public bool IsSigned { get; private set; }
    
    public RequiredDocument(DocumentType type, bool isRequired)
    {
        Type = type;
        IsRequired = isRequired;
        IsProvided = false;
        IsSigned = false;
    }
    
    public void MarkAsProvided() => IsProvided = true;
    public void MarkAsSigned() => IsSigned = true;
}

/// <summary>
/// DEPRECATED: Use LocaGuest.Domain.Aggregates.PaymentAggregate.Payment instead
/// This class is kept for backward compatibility
/// </summary>
[Obsolete("Use PaymentAggregate.Payment instead")]
public class ContractPayment : Entity
{
    public string Code { get; private set; } = string.Empty;  // T0001-PAY0001
    
    public Guid ContractId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public ContractPaymentMethod Method { get; private set; }
    public ContractPaymentStatus Status { get; private set; }

    private ContractPayment() { } // EF

    internal static ContractPayment Create(Guid contractId, decimal amount, DateTime paymentDate, ContractPaymentMethod method)
    {
        return new ContractPayment
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            Amount = amount,
            PaymentDate = paymentDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(paymentDate, DateTimeKind.Utc) : paymentDate.ToUniversalTime(),
            Method = method,
            Status = ContractPaymentStatus.Completed
        };
    }

    public void SetCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(Code))
            throw new InvalidOperationException("Code cannot be changed once set");
        Code = code;
    }

    internal void MarkAsLate()
    {
        Status = ContractPaymentStatus.Late;
    }
}

[Obsolete("Use PaymentAggregate.PaymentMethod instead")]
public enum ContractPaymentMethod
{
    BankTransfer,
    Check,
    Cash,
    CreditCard
}

[Obsolete("Use PaymentAggregate.PaymentStatus instead")]
public enum ContractPaymentStatus
{
    Pending,
    Completed,
    Failed,
    Late
}
