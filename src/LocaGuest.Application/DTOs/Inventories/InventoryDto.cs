namespace LocaGuest.Application.DTOs.Inventories;

/// <summary>
/// DTO pour un état des lieux d'entrée
/// </summary>
public class InventoryEntryDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid ContractId { get; set; }
    public Guid RenterTenantId { get; set; }
    public DateTime InspectionDate { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public bool TenantPresent { get; set; }
    public string? RepresentativeName { get; set; }
    public string? GeneralObservations { get; set; }
    public List<InventoryItemDto> Items { get; set; } = new();
    public List<string> PhotoUrls { get; set; } = new();
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// EDL finalisé = document légal, non modifiable
    /// </summary>
    public bool IsFinalized { get; set; }

    /// <summary>
    /// Date de finalisation (signature)
    /// </summary>
    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO pour un élément d'inventaire
/// </summary>
public class InventoryItemDto
{
    public string RoomName { get; set; } = string.Empty;
    public string ElementName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Murs, Sol, Plafond, Équipement
    public string Condition { get; set; } = "Good"; // New, Good, Fair, Poor, Damaged
    public string? Comment { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
}

/// <summary>
/// DTO pour un état des lieux de sortie
/// </summary>
public class InventoryExitDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid ContractId { get; set; }
    public Guid RenterTenantId { get; set; }
    public Guid InventoryEntryId { get; set; }
    public DateTime InspectionDate { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public bool TenantPresent { get; set; }
    public string? RepresentativeName { get; set; }
    public string? GeneralObservations { get; set; }
    public List<InventoryComparisonDto> Comparisons { get; set; } = new();
    public List<DegradationDto> Degradations { get; set; } = new();
    public List<string> PhotoUrls { get; set; } = new();
    public decimal TotalDeductionAmount { get; set; }
    public decimal? OwnerCoveredAmount { get; set; }
    public string? FinancialNotes { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO pour une comparaison entre entrée et sortie
/// </summary>
public class InventoryComparisonDto
{
    public string RoomName { get; set; } = string.Empty;
    public string ElementName { get; set; } = string.Empty;
    public string EntryCondition { get; set; } = "Good";
    public string ExitCondition { get; set; } = "Good";
    public bool HasDegradation { get; set; }
    public string? Comment { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
}

/// <summary>
/// DTO pour une dégradation
/// </summary>
public class DegradationDto
{
    public string RoomName { get; set; } = string.Empty;
    public string ElementName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsImputedToTenant { get; set; }
    public decimal EstimatedCost { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
}

/// <summary>
/// DTO pour créer un état des lieux d'entrée
/// </summary>
public class CreateInventoryEntryDto
{
    public Guid PropertyId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid ContractId { get; set; }
    public DateTime InspectionDate { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public bool TenantPresent { get; set; }
    public string? RepresentativeName { get; set; }
    public string? GeneralObservations { get; set; }
    public List<InventoryItemDto> Items { get; set; } = new();
    public List<string> PhotoUrls { get; set; } = new();
}

/// <summary>
/// DTO pour créer un état des lieux de sortie
/// </summary>
public class CreateInventoryExitDto
{
    public Guid PropertyId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid ContractId { get; set; }
    public Guid InventoryEntryId { get; set; }
    public DateTime InspectionDate { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public bool TenantPresent { get; set; }
    public string? RepresentativeName { get; set; }
    public string? GeneralObservations { get; set; }
    public List<InventoryComparisonDto> Comparisons { get; set; } = new();
    public List<DegradationDto> Degradations { get; set; } = new();
    public List<string> PhotoUrls { get; set; } = new();
    public decimal? OwnerCoveredAmount { get; set; }
    public string? FinancialNotes { get; set; }
}
