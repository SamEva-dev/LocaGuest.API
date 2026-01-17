using LocaGuest.Application.DTOs.Contracts;

namespace LocaGuest.Api.Tests.Builders;

public class ContractDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _propertyId = Guid.NewGuid();
    private Guid _OccupantId = Guid.NewGuid();
    private string? _propertyName = "Test Property";
    private string? _OccupantName = "John Doe";
    private string _type = "Unfurnished";
    private DateTime _startDate = DateTime.UtcNow;
    private DateTime _endDate = DateTime.UtcNow.AddYears(1);
    private decimal _rent = 1200;
    private decimal? _deposit = 2400;
    private string _status = "Active";
    private string? _notes = "Test contract notes";
    private int _paymentsCount = 12;
    private DateTime _createdAt = DateTime.UtcNow;

    public ContractDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ContractDtoBuilder WithPropertyId(Guid propertyId)
    {
        _propertyId = propertyId;
        return this;
    }

    public ContractDtoBuilder WithOccupantId(Guid OccupantId)
    {
        _OccupantId = OccupantId;
        return this;
    }

    public ContractDtoBuilder WithPropertyName(string? propertyName)
    {
        _propertyName = propertyName;
        return this;
    }

    public ContractDtoBuilder WithOccupantName(string? OccupantName)
    {
        _OccupantName = OccupantName;
        return this;
    }

    public ContractDtoBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    public ContractDtoBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public ContractDtoBuilder WithRent(decimal rent)
    {
        _rent = rent;
        return this;
    }

    public ContractDto Build()
    {
        return new ContractDto
        {
            Id = _id,
            PropertyId = _propertyId,
            OccupantId = _OccupantId,
            PropertyName = _propertyName,
            OccupantName = _OccupantName,
            Type = _type,
            StartDate = _startDate,
            EndDate = _endDate,
            Rent = _rent,
            Deposit = _deposit,
            Status = _status,
            Notes = _notes,
            PaymentsCount = _paymentsCount,
            CreatedAt = _createdAt
        };
    }

    public static ContractDtoBuilder AContract() => new();
}
