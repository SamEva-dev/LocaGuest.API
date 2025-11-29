using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.InventoryAggregate;

/// <summary>
/// Représente un élément inspecté dans un état des lieux (murs, sol, équipement, etc.)
/// Value Object
/// </summary>
public class InventoryItem : ValueObject
{
    public string RoomName { get; private set; } = string.Empty;
    public string ElementName { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty; // Murs, Sol, Plafond, Équipement
    public InventoryCondition Condition { get; private set; }
    public string? Comment { get; private set; }
    public List<string> PhotoUrls { get; private set; } = new();

    private InventoryItem() { } // EF Core

    public static InventoryItem Create(
        string roomName,
        string elementName,
        string category,
        InventoryCondition condition,
        string? comment = null,
        List<string>? photoUrls = null)
    {
        return new InventoryItem
        {
            RoomName = roomName,
            ElementName = elementName,
            Category = category,
            Condition = condition,
            Comment = comment,
            PhotoUrls = photoUrls ?? new List<string>()
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RoomName;
        yield return ElementName;
        yield return Category;
        yield return Condition;
    }
}

/// <summary>
/// État d'un élément dans l'EDL
/// </summary>
public enum InventoryCondition
{
    New,        // Neuf
    Good,       // Bon
    Fair,       // Moyen
    Poor,       // Mauvais
    Damaged     // Dégradé
}
