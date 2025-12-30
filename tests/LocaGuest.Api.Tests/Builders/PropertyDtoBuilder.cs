using LocaGuest.Application.DTOs.Properties;

namespace LocaGuest.Api.Tests.Builders;

/// <summary>
/// Builder for PropertyDto - used in list queries
/// </summary>
public class PropertyDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Property";
    private string _address = "123 Test Street";
    private string? _city = "Paris";
    private string? _postalCode = "75001";
    private string? _country = "France";
    private string _type = "Apartment";
    private decimal _surface = 75.5m;
    private int? _bedrooms = 2;
    private int? _bathrooms = 1;
    private int? _floor = 3;
    private bool _hasElevator = true;
    private bool _hasParking = false;
    private decimal _rent = 1200;
    private decimal? _charges = 150;
    private string _status = "Vacant";
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _updatedAt = null;

    public PropertyDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public PropertyDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PropertyDtoBuilder WithAddress(string address)
    {
        _address = address;
        return this;
    }

    public PropertyDtoBuilder WithCity(string? city)
    {
        _city = city;
        return this;
    }

    public PropertyDtoBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    public PropertyDtoBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public PropertyDtoBuilder WithRent(decimal rent)
    {
        _rent = rent;
        return this;
    }

    public PropertyDtoBuilder WithSurface(decimal surface)
    {
        _surface = surface;
        return this;
    }

    public PropertyDto Build()
    {
        return new PropertyDto
        {
            Id = _id,
            Name = _name,
            Address = _address,
            City = _city,
            PostalCode = _postalCode,
            Country = _country,
            Type = _type,
            Surface = _surface,
            Bedrooms = _bedrooms,
            Bathrooms = _bathrooms,
            Floor = _floor,
            HasElevator = _hasElevator,
            HasParking = _hasParking,
            Rent = _rent,
            Charges = _charges,
            Status = _status,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt
        };
    }

    public static PropertyDtoBuilder AProperty() => new();
}

/// <summary>
/// Builder for PropertyDetailDto - used in single property queries
/// </summary>
public class PropertyDetailDtoBuilder : PropertyDtoBuilder
{
    private decimal? _purchasePrice = 250000;
    private List<string> _features = new() { "Wifi", "Climatisation" };
    private int _activeContractsCount = 1;
    private decimal _totalRevenue = 15000;

    public PropertyDetailDtoBuilder WithPurchasePrice(decimal? purchasePrice)
    {
        _purchasePrice = purchasePrice;
        return this;
    }

    public PropertyDetailDtoBuilder WithActiveContractsCount(int count)
    {
        _activeContractsCount = count;
        return this;
    }

    public new PropertyDetailDto Build()
    {
        var baseDto = base.Build();
        return new PropertyDetailDto
        {
            Id = baseDto.Id,
            Name = baseDto.Name,
            Address = baseDto.Address,
            City = baseDto.City,
            PostalCode = baseDto.PostalCode,
            Country = baseDto.Country,
            Type = baseDto.Type,
            Surface = baseDto.Surface,
            Bedrooms = baseDto.Bedrooms,
            Bathrooms = baseDto.Bathrooms,
            Floor = baseDto.Floor,
            HasElevator = baseDto.HasElevator,
            HasParking = baseDto.HasParking,
            Rent = baseDto.Rent,
            Charges = baseDto.Charges,
            Status = baseDto.Status,
            CreatedAt = baseDto.CreatedAt,
            UpdatedAt = baseDto.UpdatedAt,
            PurchasePrice = _purchasePrice,
            Features = _features,
            ActiveContractsCount = _activeContractsCount,
            TotalRevenue = _totalRevenue
        };
    }

    public static new PropertyDetailDtoBuilder AProperty() => new();
}
