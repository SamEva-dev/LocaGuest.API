using LocaGuest.Domain.Common;
using LocaGuest.Domain.Exceptions;

namespace LocaGuest.Domain.Aggregates.InventoryAggregate;

/// <summary>
/// État des lieux d'ENTRÉE
/// Aggregate Root
/// </summary>
public class InventoryEntry : AuditableEntity
{
    public Guid PropertyId { get; private set; }
    public Guid? RoomId { get; private set; } // Pour colocation
    public Guid ContractId { get; private set; }
    
    /// <summary>
    /// ID du locataire (Tenant entity) - ne pas confondre avec TenantId multi-tenant hérité de AuditableEntity
    /// </summary>
    public Guid RenterOccupantId { get; private set; }
    
    public DateTime InspectionDate { get; private set; }
    public string AgentName { get; private set; } = string.Empty;
    public bool TenantPresent { get; private set; }
    public string? RepresentativeName { get; private set; }
    public string? GeneralObservations { get; private set; }
    
    private readonly List<InventoryItem> _items = new();
    public IReadOnlyCollection<InventoryItem> Items => _items.AsReadOnly();
    
    public List<string> PhotoUrls { get; private set; } = new();
    
    public InventoryStatus Status { get; private set; }
    
    /// <summary>
    /// EDL finalisé = document légal, ne peut plus être modifié ni supprimé
    /// </summary>
    public bool IsFinalized { get; private set; }
    
    /// <summary>
    /// Date de finalisation (signature)
    /// </summary>
    public DateTime? FinalizedAt { get; private set; }
    
    private InventoryEntry() { } // EF Core

    public static InventoryEntry Create(
        Guid propertyId,
        Guid contractId,
        Guid renterTenantId,
        DateTime inspectionDate,
        string agentName,
        bool tenantPresent,
        Guid? roomId = null,
        string? representativeName = null,
        string? generalObservations = null)
    {
        if (string.IsNullOrWhiteSpace(agentName))
            throw new ValidationException("INVENTORY_AGENT_REQUIRED", "Agent name is required");
            
        if (inspectionDate > DateTime.UtcNow.AddDays(30))
            throw new ValidationException("INVENTORY_INVALID_DATE", "Inspection date cannot be too far in the future");

        return new InventoryEntry
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            RoomId = roomId,
            ContractId = contractId,
            RenterOccupantId = renterTenantId,
            InspectionDate = inspectionDate,
            AgentName = agentName,
            TenantPresent = tenantPresent,
            RepresentativeName = representativeName,
            GeneralObservations = generalObservations,
            Status = InventoryStatus.Draft,
            IsFinalized = false
        };
    }

    public void AddItem(InventoryItem item)
    {
        if (IsFinalized)
            throw new ValidationException("INVENTORY_FINALIZED", "Cannot modify a finalized inventory - legal document");
            
        _items.Add(item);
    }

    public void AddItems(IEnumerable<InventoryItem> items)
    {
        foreach (var item in items)
        {
            AddItem(item);
        }
    }

    public void AddPhoto(string photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
            throw new ArgumentException("Photo URL cannot be empty", nameof(photoUrl));
            
        PhotoUrls.Add(photoUrl);
    }

    public void Complete()
    {
        if (_items.Count == 0)
            throw new ValidationException("INVENTORY_NO_ITEMS", "Cannot complete an inventory without items");
            
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
            
        if (_items.Count == 0)
            throw new ValidationException("INVENTORY_NO_ITEMS", "Cannot finalize an inventory without items");
            
        IsFinalized = true;
        FinalizedAt = DateTime.UtcNow;
        Status = InventoryStatus.Completed;
    }
    
    /// <summary>
    /// Vérifier si l'EDL peut être modifié
    /// </summary>
    public bool CanBeModified()
    {
        return !IsFinalized;
    }
    
    /// <summary>
    /// Vérifier si l'EDL peut être supprimé
    /// </summary>
    public bool CanBeDeleted(bool contractIsActive, bool exitExists)
    {
        // EDL finalisé = JAMAIS supprimable
        if (IsFinalized)
            return false;
            
        // Contrat actif = JAMAIS supprimable
        if (contractIsActive)
            return false;
            
        // EDL de sortie existe = JAMAIS supprimable
        if (exitExists)
            return false;
            
        // Sinon = supprimable (Draft seulement)
        return Status == InventoryStatus.Draft;
    }

    public void UpdateGeneralInfo(
        DateTime? inspectionDate = null,
        string? agentName = null,
        bool? tenantPresent = null,
        string? representativeName = null,
        string? generalObservations = null)
    {
        if (IsFinalized)
            throw new ValidationException("INVENTORY_FINALIZED", "Cannot modify a finalized inventory - legal document");

        if (inspectionDate.HasValue)
            InspectionDate = inspectionDate.Value;
            
        if (!string.IsNullOrWhiteSpace(agentName))
            AgentName = agentName;
            
        if (tenantPresent.HasValue)
            TenantPresent = tenantPresent.Value;
            
        if (representativeName != null)
            RepresentativeName = representativeName;
            
        if (generalObservations != null)
            GeneralObservations = generalObservations;
    }
}

/// <summary>
/// Statut d'un état des lieux
/// </summary>
public enum InventoryStatus
{
    Draft,      // Brouillon (en cours de remplissage)
    Completed   // Terminé et validé
}
