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
    public decimal? Deposit { get; private set; }
    public ContractStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<Payment> _payments = new();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();
    
    private readonly List<RequiredDocument> _requiredDocuments = new();
    public IReadOnlyCollection<RequiredDocument> RequiredDocuments => _requiredDocuments.AsReadOnly();
    
    private readonly List<Guid> _documentIds = new();
    public IReadOnlyCollection<Guid> DocumentIds => _documentIds.AsReadOnly();

    private Contract() { } // EF

    public static Contract Create(
        Guid propertyId,
        Guid renterTenantId,
        ContractType type,
        DateTime startDate,
        DateTime endDate,
        decimal rent,
        decimal? deposit = null)
    {
        if (startDate >= endDate)
            throw new ValidationException("CONTRACT_INVALID_DATES", "Start date must be before end date");

        if (rent <= 0)
            throw new ValidationException("CONTRACT_INVALID_RENT", "Rent must be positive");

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            RenterTenantId = renterTenantId,
            Type = type,
            StartDate = startDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(startDate, DateTimeKind.Utc) : startDate.ToUniversalTime(),
            EndDate = endDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(endDate, DateTimeKind.Utc) : endDate.ToUniversalTime(),
            Rent = rent,
            Deposit = deposit,
            Status = ContractStatus.Draft
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

    public void Renew(DateTime newEndDate)
    {
        if (newEndDate <= EndDate)
            throw new ValidationException("CONTRACT_INVALID_RENEWAL", "New end date must be after current end date");

        EndDate = newEndDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(newEndDate, DateTimeKind.Utc) : newEndDate.ToUniversalTime();
        AddDomainEvent(new ContractRenewed(Id, PropertyId, RenterTenantId, EndDate));
    }

    public void Terminate(DateTime terminationDate)
    {
        if (Status == ContractStatus.Terminated) return;

        Status = ContractStatus.Terminated;
        AddDomainEvent(new ContractTerminated(Id, PropertyId, RenterTenantId, terminationDate));
    }

    public void MarkAsExpiring()
    {
        if (Status == ContractStatus.Active)
        {
            Status = ContractStatus.Expiring;
        }
    }

    /// <summary>
    /// Marquer le contrat comme signé (ancienne méthode, dépréciée)
    /// Transition: Draft → FullySigned
    /// Cette méthode est conservée pour compatibilité mais il est recommandé de signer les documents individuellement
    /// </summary>
    [Obsolete("Utilisez la signature de documents individuels via OnDocumentSigned")]
    public void MarkAsSigned(DateTime? signedDate = null)
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.PartialSigned)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only draft or partial signed contracts can be marked as signed");

        Status = ContractStatus.FullySigned;
        var effectiveSignedDate = signedDate ?? DateTime.UtcNow;
        AddDomainEvent(new ContractSigned(Id, PropertyId, RenterTenantId, effectiveSignedDate));
    }

    /// <summary>
    /// Activer un contrat signé
    /// Transition: FullySigned → Active
    /// </summary>
    public void Activate()
    {
        if (Status != ContractStatus.FullySigned)
            throw new ValidationException("CONTRACT_INVALID_STATUS", "Only fully signed contracts can be activated");

        if (DateTime.UtcNow < StartDate)
            throw new ValidationException("CONTRACT_NOT_STARTED", "Contract cannot be activated before start date");

        Status = ContractStatus.Active;
        AddDomainEvent(new ContractActivated(Id, PropertyId, RenterTenantId));
    }

    public Payment RecordPayment(decimal amount, DateTime paymentDate, PaymentMethod method)
    {
        if (amount <= 0)
            throw new ValidationException("PAYMENT_INVALID_AMOUNT", "Payment amount must be positive");

        var payment = Payment.Create(Id, amount, paymentDate, method);
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
    /// Vérifie si tous les documents requis sont signés pour activation automatique
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
            if (Status == ContractStatus.Draft || Status == ContractStatus.PartialSigned)
            {
                Status = ContractStatus.FullySigned;
                AddDomainEvent(new ContractFullySigned(Id, PropertyId, RenterTenantId));
                
                // Activation automatique si dans la période de validité
                if (CanActivate())
                {
                    Activate();
                }
            }
        }
        else if (Status == ContractStatus.Draft)
        {
            // Au moins un document signé mais pas tous
            Status = ContractStatus.PartialSigned;
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
    /// Vérifier si le contrat peut être activé
    /// </summary>
    public bool CanActivate()
    {
        return Status == ContractStatus.FullySigned 
            && DateTime.UtcNow >= StartDate;
    }
}

public enum ContractType
{
    Furnished,
    Unfurnished
}

public enum ContractStatus
{
    Draft,          // Brouillon, documents non créés ou non signés
    PartialSigned,  // Certains documents signés mais pas tous
    FullySigned,    // Tous les documents requis signés
    Active,         // Contrat activé
    Expiring,       // Expire bientôt
    Terminated      // Résilié
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

public class Payment : Entity
{
    public string Code { get; private set; } = string.Empty;  // T0001-PAY0001
    
    public Guid ContractId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }

    private Payment() { } // EF

    internal static Payment Create(Guid contractId, decimal amount, DateTime paymentDate, PaymentMethod method)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            Amount = amount,
            PaymentDate = paymentDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(paymentDate, DateTimeKind.Utc) : paymentDate.ToUniversalTime(),
            Method = method,
            Status = PaymentStatus.Completed
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
        Status = PaymentStatus.Late;
    }
}

public enum PaymentMethod
{
    BankTransfer,
    Check,
    Cash,
    CreditCard
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Late
}
