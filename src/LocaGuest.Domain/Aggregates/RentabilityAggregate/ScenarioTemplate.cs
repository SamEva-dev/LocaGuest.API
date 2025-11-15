using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.RentabilityAggregate;

public class ScenarioTemplate : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public bool IsPublic { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public int UsageCount { get; private set; }
    public double Rating { get; private set; }
    public int RatingCount { get; private set; }
    
    // Template data stored as JSON
    public string TemplateDataJson { get; private set; } = string.Empty;
    
    // Preview image URL
    public string? PreviewImageUrl { get; private set; }
    
    private ScenarioTemplate() { }
    
    public static ScenarioTemplate Create(
        string name,
        string description,
        string category,
        string templateDataJson,
        bool isPublic = true,
        Guid? createdByUserId = null)
    {
        return new ScenarioTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Category = category,
            TemplateDataJson = templateDataJson,
            IsPublic = isPublic,
            CreatedByUserId = createdByUserId,
            UsageCount = 0,
            Rating = 0,
            RatingCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void IncrementUsage()
    {
        UsageCount++;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void AddRating(int rating)
    {
        if (rating < 1 || rating > 5) throw new ArgumentException("Rating must be between 1 and 5");
        
        var totalRating = (Rating * RatingCount) + rating;
        RatingCount++;
        Rating = totalRating / RatingCount;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void Update(string name, string description, string category, string templateDataJson)
    {
        Name = name;
        Description = description;
        Category = category;
        TemplateDataJson = templateDataJson;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void SetPreviewImage(string url)
    {
        PreviewImageUrl = url;
        LastModifiedAt = DateTime.UtcNow;
    }
}
