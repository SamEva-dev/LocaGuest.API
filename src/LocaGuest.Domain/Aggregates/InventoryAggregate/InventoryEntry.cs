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
    public Guid TenantId { get; private set; }
    
    public DateTime InspectionDate { get; private set; }
    public string AgentName { get; private set; } = string.Empty;
    public bool TenantPresent { get; private set; }
    public string? RepresentativeName { get; private set; }
    public string? GeneralObservations { get; private set; }
    
    private readonly List<InventoryItem> _items = new();
    public IReadOnlyCollection<InventoryItem> Items => _items.AsReadOnly();
    
    public List<string> PhotoUrls { get; private set; } = new();
    
    public InventoryStatus Status { get; private set; }
    
    private InventoryEntry() { } // EF Core

    public static InventoryEntry Create(
        Guid propertyId,
        Guid contractId,
        Guid tenantId,
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
            TenantId = tenantId,
            InspectionDate = inspectionDate,
            AgentName = agentName,
            TenantPresent = tenantPresent,
            RepresentativeName = representativeName,
            GeneralObservations = generalObservations,
            Status = InventoryStatus.Draft
        };
    }

    public void AddItem(InventoryItem item)
    {
        if (Status == InventoryStatus.Completed)
            throw new ValidationException("INVENTORY_COMPLETED", "Cannot modify a completed inventory");
            
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

    public void UpdateGeneralInfo(
        DateTime? inspectionDate = null,
        string? agentName = null,
        bool? tenantPresent = null,
        string? representativeName = null,
        string? generalObservations = null)
    {
        if (Status == InventoryStatus.Completed)
            throw new ValidationException("INVENTORY_COMPLETED", "Cannot modify a completed inventory");

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
