namespace LocaGuest.Domain.Aggregates.AnalyticsAggregate;

public class SatisfactionFeedback
{
    public Guid Id { get; private set; }

    public Guid? OrganizationId { get; private set; }

    public Guid? UserId { get; private set; }

    public int Rating { get; private set; }

    public string? Comment { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private SatisfactionFeedback() { }

    public static SatisfactionFeedback Create(
        int rating,
        string? comment,
        Guid? organizationId = null,
        Guid? userId = null)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5");
        }

        return new SatisfactionFeedback
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            Rating = rating,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
