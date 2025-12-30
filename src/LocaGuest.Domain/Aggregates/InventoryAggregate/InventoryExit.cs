using LocaGuest.Domain.Common;
using LocaGuest.Domain.Exceptions;

namespace LocaGuest.Domain.Aggregates.InventoryAggregate;

/// <summary>
/// État des lieux de SORTIE avec comparaison
/// Aggregate Root
/// </summary>
public class InventoryExit : AuditableEntity
{
    public Guid PropertyId { get; private set; }
    public Guid? RoomId { get; private set; }
    public Guid ContractId { get; private set; }
    
    /// <summary>
    /// ID du locataire (Tenant entity) - ne pas confondre avec TenantId multi-tenant hérité de AuditableEntity
    /// </summary>
    public Guid RenterTenantId { get; private set; }
    public Guid InventoryEntryId { get; private set; } // Référence à l'EDL d'entrée
    
    public DateTime InspectionDate { get; private set; }
    public string AgentName { get; private set; } = string.Empty;
    public bool TenantPresent { get; private set; }
    public string? RepresentativeName { get; private set; }
    public string? GeneralObservations { get; private set; }
    
    private readonly List<InventoryComparison> _comparisons = new();
    public IReadOnlyCollection<InventoryComparison> Comparisons => _comparisons.AsReadOnly();
    
    private readonly List<Degradation> _degradations = new();
    public IReadOnlyCollection<Degradation> Degradations => _degradations.AsReadOnly();
    
    public List<string> PhotoUrls { get; private set; } = new();
    
    public decimal TotalDeductionAmount { get; private set; }
    public decimal? OwnerCoveredAmount { get; private set; }
    public string? FinancialNotes { get; private set; }
    
    public InventoryStatus Status { get; private set; }

    /// <summary>
    /// EDL finalisé = document légal, ne peut plus être modifié ni supprimé
    /// </summary>
    public bool IsFinalized { get; private set; }

    /// <summary>
    /// Date de finalisation (signature)
    /// </summary>
    public DateTime? FinalizedAt { get; private set; }
    
    private InventoryExit() { } // EF Core

    public static InventoryExit Create(
        Guid propertyId,
        Guid contractId,
        Guid renterTenantId,
        Guid inventoryEntryId,
        DateTime inspectionDate,
        string agentName,
        bool tenantPresent,
        Guid? roomId = null,
        string? representativeName = null,
        string? generalObservations = null)
    {
        if (string.IsNullOrWhiteSpace(agentName))
            throw new ValidationException("INVENTORY_AGENT_REQUIRED", "Agent name is required");

        return new InventoryExit
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            RoomId = roomId,
            ContractId = contractId,
            RenterTenantId = renterTenantId,
            InventoryEntryId = inventoryEntryId,
            InspectionDate = inspectionDate,
            AgentName = agentName,
            TenantPresent = tenantPresent,
            RepresentativeName = representativeName,
            GeneralObservations = generalObservations,
            Status = InventoryStatus.Draft,
            TotalDeductionAmount = 0,
            IsFinalized = false,
            FinalizedAt = null
        };
    }

    public void AddComparison(InventoryComparison comparison)
    {
        if (IsFinalized || Status == InventoryStatus.Completed)
            throw new ValidationException("INVENTORY_COMPLETED", "Cannot modify a completed inventory");
            
        _comparisons.Add(comparison);
    }

    public void AddDegradation(Degradation degradation)
    {
        if (IsFinalized || Status == InventoryStatus.Completed)
            throw new ValidationException("INVENTORY_COMPLETED", "Cannot modify a completed inventory");
            
        _degradations.Add(degradation);
        RecalculateTotalDeduction();
    }

    public void AddPhoto(string photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
            throw new ArgumentException("Photo URL cannot be empty", nameof(photoUrl));
            
        PhotoUrls.Add(photoUrl);
    }

    public void SetFinancialInfo(decimal? ownerCoveredAmount, string? financialNotes)
    {
        if (IsFinalized)
            throw new ValidationException("INVENTORY_FINALIZED", "Cannot modify a finalized inventory - legal document");

        OwnerCoveredAmount = ownerCoveredAmount;
        FinancialNotes = financialNotes;
        RecalculateTotalDeduction();
    }

    private void RecalculateTotalDeduction()
    {
        var totalDegradations = _degradations
            .Where(d => d.IsImputedToTenant)
            .Sum(d => d.EstimatedCost);
            
        var ownerCovered = OwnerCoveredAmount ?? 0;
        TotalDeductionAmount = Math.Max(0, totalDegradations - ownerCovered);
    }

    public void Complete()
    {
        if (_comparisons.Count == 0)
            throw new ValidationException("INVENTORY_NO_COMPARISONS", "Cannot complete an inventory without comparisons");
            
        Status = InventoryStatus.Completed;
    }

    /// <summary>
    /// Finaliser l'EDL = signer et verrouiller
    /// Devient un document légal opposable
    /// </summary>
    public void MarkAsFinalized()
    {
        if (IsFinalized)
            throw new ValidationException("INVENTORY_ALREADY_FINALIZED", "This inventory is already finalized");

        if (_comparisons.Count == 0)
            throw new ValidationException("INVENTORY_NO_COMPARISONS", "Cannot finalize an inventory without comparisons");

        IsFinalized = true;
        FinalizedAt = DateTime.UtcNow;
        Status = InventoryStatus.Completed;
    }
}

/// <summary>
/// Comparaison entre l'état d'entrée et l'état de sortie pour un élément
/// Value Object
/// </summary>
public class InventoryComparison : ValueObject
{
    public string RoomName { get; private set; } = string.Empty;
    public string ElementName { get; private set; } = string.Empty;
    public InventoryCondition EntryCondition { get; private set; }
    public InventoryCondition ExitCondition { get; private set; }
    public bool HasDegradation => ExitCondition < EntryCondition;
    public string? Comment { get; private set; }
    public List<string> PhotoUrls { get; private set; } = new();

    private InventoryComparison() { } // EF Core

    public static InventoryComparison Create(
        string roomName,
        string elementName,
        InventoryCondition entryCondition,
        InventoryCondition exitCondition,
        string? comment = null,
        List<string>? photoUrls = null)
    {
        return new InventoryComparison
        {
            RoomName = roomName,
            ElementName = elementName,
            EntryCondition = entryCondition,
            ExitCondition = exitCondition,
            Comment = comment,
            PhotoUrls = photoUrls ?? new List<string>()
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RoomName;
        yield return ElementName;
        yield return EntryCondition;
        yield return ExitCondition;
    }
}

/// <summary>
/// Dégradation constatée imputable au locataire
/// Value Object
/// </summary>
public class Degradation : ValueObject
{
    public string RoomName { get; private set; } = string.Empty;
    public string ElementName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsImputedToTenant { get; private set; }
    public decimal EstimatedCost { get; private set; }
    public List<string> PhotoUrls { get; private set; } = new();

    private Degradation() { } // EF Core

    public static Degradation Create(
        string roomName,
        string elementName,
        string description,
        bool isImputedToTenant,
        decimal estimatedCost,
        List<string>? photoUrls = null)
    {
        if (estimatedCost < 0)
            throw new ValidationException("DEGRADATION_INVALID_COST", "Cost cannot be negative");

        return new Degradation
        {
            RoomName = roomName,
            ElementName = elementName,
            Description = description,
            IsImputedToTenant = isImputedToTenant,
            EstimatedCost = estimatedCost,
            PhotoUrls = photoUrls ?? new List<string>()
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RoomName;
        yield return ElementName;
        yield return Description;
        yield return IsImputedToTenant;
        yield return EstimatedCost;
    }
}
