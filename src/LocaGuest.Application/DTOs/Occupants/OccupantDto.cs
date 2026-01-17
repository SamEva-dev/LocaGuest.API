namespace LocaGuest.Application.DTOs.Occupants;

public class OccupantDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;  // ✅ T0001-L0001
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Status { get; set; } = "Active";
    public int ActiveContracts { get; set; }
    public DateTime? MoveInDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // ✅ Dossier administratif
    public bool HasIdentityDocument { get; set; }
    
    // ⭐ Association Occupant ↔ Property
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
}

public class OccupantDetailDto : OccupantDto
{
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Nationality { get; set; }
    public string? IdNumber { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Occupation { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public string? Notes { get; set; }
}

public class CreateOccupantDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Nationality { get; set; }
    public string? IdNumber { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Occupation { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public string? Notes { get; set; }
}

public class UpdateOccupantDto : CreateOccupantDto
{
    public Guid Id { get; set; }
}
