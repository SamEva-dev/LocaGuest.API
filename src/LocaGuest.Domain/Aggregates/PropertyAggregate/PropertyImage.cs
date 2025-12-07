using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.PropertyAggregate;

/// <summary>
/// Représente une image ou un document associé à une propriété
/// </summary>
public class PropertyImage : Entity
{
    public Guid PropertyId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string Category { get; private set; } = "other"; // exterior, living_room, kitchen, bedroom, bathroom, other
    public string MimeType { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    
    private PropertyImage() { } // EF Core

    public PropertyImage(
        Guid propertyId, 
        string fileName, 
        string filePath, 
        long fileSize, 
        string mimeType,
        string category = "other")
    {
        Id = Guid.NewGuid();
        PropertyId = propertyId;
        FileName = fileName;
        FilePath = filePath;
        FileSize = fileSize;
        MimeType = mimeType;
        Category = category;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateCategory(string category)
    {
        Category = category;
    }
}
