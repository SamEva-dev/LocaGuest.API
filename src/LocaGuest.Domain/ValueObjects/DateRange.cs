using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.ValueObjects;

/// <summary>
/// Value Object representing a date range (period)
/// </summary>
public sealed class DateRange : ValueObject
{
    public DateTime Start { get; }
    public DateTime? End { get; }

    private DateRange(DateTime start, DateTime? end)
    {
        Start = start;
        End = end;
    }

    public static DateRange Create(DateTime start, DateTime? end = null)
    {
        if (end.HasValue && end.Value < start)
            throw new ArgumentException("End date cannot be before start date");

        return new DateRange(start, end);
    }

    public int DurationInDays()
    {
        if (!End.HasValue)
            return (DateTime.UtcNow.Date - Start.Date).Days;

        return (End.Value.Date - Start.Date).Days;
    }

    public int DurationInMonths()
    {
        var endDate = End ?? DateTime.UtcNow;
        return ((endDate.Year - Start.Year) * 12) + endDate.Month - Start.Month;
    }

    public bool Overlaps(DateRange other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        var thisEnd = End ?? DateTime.MaxValue;
        var otherEnd = other.End ?? DateTime.MaxValue;

        return Start < otherEnd && other.Start < thisEnd;
    }

    public bool Contains(DateTime date)
    {
        if (End.HasValue)
            return date >= Start && date <= End.Value;

        return date >= Start;
    }

    public bool IsActive()
    {
        var now = DateTime.UtcNow;
        return now >= Start && (!End.HasValue || now <= End.Value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString()
    {
        if (End.HasValue)
            return $"{Start:yyyy-MM-dd} to {End.Value:yyyy-MM-dd}";

        return $"{Start:yyyy-MM-dd} (ongoing)";
    }
}
