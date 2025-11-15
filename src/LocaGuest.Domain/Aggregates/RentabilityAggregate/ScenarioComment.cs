using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.RentabilityAggregate;

public class ScenarioComment : AuditableEntity
{
    public Guid ScenarioId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public Guid? ParentCommentId { get; private set; }
    public bool IsEdited { get; private set; }
    public DateTime? EditedAt { get; private set; }
    
    private ScenarioComment() { }
    
    public static ScenarioComment Create(
        Guid scenarioId,
        Guid userId,
        string userName,
        string content,
        Guid? parentCommentId = null)
    {
        return new ScenarioComment
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            UserId = userId,
            UserName = userName,
            Content = content,
            ParentCommentId = parentCommentId,
            IsEdited = false,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void UpdateContent(string newContent)
    {
        Content = newContent;
        IsEdited = true;
        EditedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }
}
