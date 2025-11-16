using LocaGuest.Application.DTOs.Tenants;

namespace LocaGuest.Api.Tests.Builders;

/// <summary>
/// Builder for TenantDto - used in list queries
/// </summary>
public class TenantDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _fullName = "John Doe";
    private string _email = "john.doe@test.com";
    private string? _phone = "+33612345678";
    private DateTime? _dateOfBirth = new DateTime(1990, 1, 1);
    private string _status = "Active";
    private int _activeContracts = 1;
    private DateTime? _moveInDate = DateTime.UtcNow.AddMonths(-6);
    private DateTime _createdAt = DateTime.UtcNow;

    public TenantDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public TenantDtoBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public TenantDtoBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public TenantDtoBuilder WithPhone(string? phone)
    {
        _phone = phone;
        return this;
    }

    public TenantDtoBuilder WithDateOfBirth(DateTime? dateOfBirth)
    {
        _dateOfBirth = dateOfBirth;
        return this;
    }

    public TenantDtoBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public TenantDtoBuilder WithActiveContracts(int count)
    {
        _activeContracts = count;
        return this;
    }

    public TenantDto Build()
    {
        return new TenantDto
        {
            Id = _id,
            FullName = _fullName,
            Email = _email,
            Phone = _phone,
            DateOfBirth = _dateOfBirth,
            Status = _status,
            ActiveContracts = _activeContracts,
            MoveInDate = _moveInDate,
            CreatedAt = _createdAt
        };
    }

    public static TenantDtoBuilder ATenant() => new();
}

/// <summary>
/// Builder for TenantDetailDto - used in single tenant queries and create/update
/// </summary>
public class TenantDetailDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _fullName = "John Doe";
    private string _email = "john.doe@test.com";
    private string? _phone = "+33612345678";
    private DateTime? _dateOfBirth = new DateTime(1990, 1, 1);
    private string _status = "Active";
    private int _activeContracts = 1;
    private DateTime? _moveInDate = DateTime.UtcNow.AddMonths(-6);
    private DateTime _createdAt = DateTime.UtcNow;
    private string? _address = "123 Test Street";
    private string? _city = "Paris";
    private string? _postalCode = "75001";
    private string? _country = "France";
    private string? _nationality = "French";
    private string? _idNumber = "123456789";
    private string? _emergencyContact = "Jane Doe";
    private string? _emergencyPhone = "+33687654321";
    private string? _occupation = "Software Engineer";
    private decimal? _monthlyIncome = 3500;
    private string? _notes = "Test notes";

    public TenantDetailDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public TenantDetailDtoBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public TenantDetailDtoBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public TenantDetailDtoBuilder WithPhone(string? phone)
    {
        _phone = phone;
        return this;
    }

    public TenantDetailDtoBuilder WithDateOfBirth(DateTime? dateOfBirth)
    {
        _dateOfBirth = dateOfBirth;
        return this;
    }

    public TenantDetailDtoBuilder WithAddress(string? address)
    {
        _address = address;
        return this;
    }

    public TenantDetailDtoBuilder WithCity(string? city)
    {
        _city = city;
        return this;
    }

    public TenantDetailDtoBuilder WithOccupation(string? occupation)
    {
        _occupation = occupation;
        return this;
    }

    public TenantDetailDtoBuilder WithMonthlyIncome(decimal? monthlyIncome)
    {
        _monthlyIncome = monthlyIncome;
        return this;
    }

    public TenantDetailDtoBuilder WithNotes(string? notes)
    {
        _notes = notes;
        return this;
    }

    public TenantDetailDto Build()
    {
        return new TenantDetailDto
        {
            Id = _id,
            FullName = _fullName,
            Email = _email,
            Phone = _phone,
            DateOfBirth = _dateOfBirth,
            Status = _status,
            ActiveContracts = _activeContracts,
            MoveInDate = _moveInDate,
            CreatedAt = _createdAt,
            Address = _address,
            City = _city,
            PostalCode = _postalCode,
            Country = _country,
            Nationality = _nationality,
            IdNumber = _idNumber,
            EmergencyContact = _emergencyContact,
            EmergencyPhone = _emergencyPhone,
            Occupation = _occupation,
            MonthlyIncome = _monthlyIncome,
            Notes = _notes
        };
    }

    public static TenantDetailDtoBuilder ATenant() => new();
}
