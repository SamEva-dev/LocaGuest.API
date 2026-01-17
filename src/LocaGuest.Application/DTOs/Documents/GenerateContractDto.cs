namespace LocaGuest.Application.DTOs.Documents;

public record GenerateContractDto
{
    /// <summary>
    /// ID du contrat auquel associer le document généré
    /// Si null, le document sera créé sans association contractuelle
    /// </summary>
    public Guid? ContractId { get; init; }
    
    public Guid OccupantId { get; init; }
    public Guid PropertyId { get; init; }
    public string ContractType { get; init; } = string.Empty; // BAIL, AVENANT, ETAT_LIEUX_ENTREE, ETAT_LIEUX_SORTIE
    public string StartDate { get; init; } = string.Empty; // ISO 8601
    public string EndDate { get; init; } = string.Empty; // ISO 8601
    public decimal Rent { get; init; }
    public decimal? Deposit { get; init; }
    public decimal? Charges { get; init; }
    public string? AdditionalClauses { get; init; }
    public bool IsThirdPartyLandlord { get; init; }
    public LandlordInfoDto? LandlordInfo { get; init; }
}

public record LandlordInfoDto
{
    public string? CompanyName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Address { get; init; }
    public string? Siret { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}
