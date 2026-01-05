namespace LocaGuest.Domain.Aggregates.PropertyAggregate;

public sealed class PropertyAirbnbSettings
{
    public int? MinimumStay { get; private set; }
    public int? MaximumStay { get; private set; }
    public decimal? PricePerNight { get; private set; }
    public int? NightsBookedPerMonth { get; private set; }

    internal PropertyAirbnbSettings() { }

    internal void Set(int minimumStay, int maximumStay, decimal pricePerNight)
    {
        MinimumStay = minimumStay;
        MaximumStay = maximumStay;
        PricePerNight = pricePerNight;
    }

    internal void Update(int? minimumStay = null, int? maximumStay = null, decimal? pricePerNight = null)
    {
        if (minimumStay.HasValue) MinimumStay = minimumStay;
        if (maximumStay.HasValue) MaximumStay = maximumStay;
        if (pricePerNight.HasValue) PricePerNight = pricePerNight;
    }

    internal void SetNightsBookedPerMonth(int? nightsBookedPerMonth)
    {
        NightsBookedPerMonth = nightsBookedPerMonth;
    }
}
