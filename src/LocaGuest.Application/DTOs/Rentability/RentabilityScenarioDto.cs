namespace LocaGuest.Application.DTOs.Rentability;

public record RentabilityScenarioDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsBase { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastModifiedAt { get; init; }
    
    public RentabilityInputDto Input { get; init; } = new();
    public string? ResultsJson { get; init; }
}

public record RentabilityInputDto
{
    public PropertyContextDto Context { get; init; } = new();
    public RevenueAssumptionsDto Revenues { get; init; } = new();
    public ChargesAssumptionsDto Charges { get; init; } = new();
    public FinancingAssumptionsDto Financing { get; init; } = new();
    public TaxAssumptionsDto Tax { get; init; } = new();
    public ExitAssumptionsDto Exit { get; init; } = new();
}

public record PropertyContextDto
{
    public string Type { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public decimal Surface { get; init; }
    public string State { get; init; } = string.Empty;
    public string Strategy { get; init; } = string.Empty;
    public int Horizon { get; init; }
    public string Objective { get; init; } = string.Empty;
    public decimal PurchasePrice { get; init; }
    public decimal NotaryFees { get; init; }
    public decimal RenovationCost { get; init; }
    public decimal? LandValue { get; init; }
    public decimal? FurnitureCost { get; init; }
}

public record RevenueAssumptionsDto
{
    public decimal MonthlyRent { get; init; }
    public string Indexation { get; init; } = string.Empty;
    public decimal IndexationRate { get; init; }
    public decimal VacancyRate { get; init; }
    public bool SeasonalityEnabled { get; init; }
    public decimal? HighSeasonMultiplier { get; init; }
    public decimal? ParkingRent { get; init; }
    public decimal? StorageRent { get; init; }
    public decimal? OtherRevenues { get; init; }
    public bool GuaranteedRent { get; init; }
    public decimal? RelocationIncrease { get; init; }
}

public record ChargesAssumptionsDto
{
    public decimal CondoFees { get; init; }
    public decimal Insurance { get; init; }
    public decimal PropertyTax { get; init; }
    public decimal ManagementFees { get; init; }
    public decimal MaintenanceRate { get; init; }
    public decimal RecoverableCharges { get; init; }
    public decimal ChargesIncrease { get; init; }
    public List<PlannedCapexDto>? PlannedCapex { get; init; }
}

public record PlannedCapexDto
{
    public int Year { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
}

public record FinancingAssumptionsDto
{
    public decimal LoanAmount { get; init; }
    public string LoanType { get; init; } = string.Empty;
    public decimal InterestRate { get; init; }
    public int Duration { get; init; }
    public decimal InsuranceRate { get; init; }
    public int DeferredMonths { get; init; }
    public string DeferredType { get; init; } = string.Empty;
    public decimal EarlyRepaymentPenalty { get; init; }
    public bool IncludeNotaryInLoan { get; init; }
    public bool IncludeRenovationInLoan { get; init; }
}

public record TaxAssumptionsDto
{
    public string Regime { get; init; } = string.Empty;
    public decimal MarginalTaxRate { get; init; }
    public decimal SocialContributions { get; init; }
    public int? DepreciationYears { get; init; }
    public int? FurnitureDepreciationYears { get; init; }
    public bool DeficitCarryForward { get; init; }
    public bool CrlApplicable { get; init; }
}

public record ExitAssumptionsDto
{
    public string Method { get; init; } = string.Empty;
    public decimal? TargetCapRate { get; init; }
    public decimal? AnnualAppreciation { get; init; }
    public decimal? TargetPricePerSqm { get; init; }
    public decimal SellingCosts { get; init; }
    public decimal CapitalGainsTax { get; init; }
    public int HoldYears { get; init; }
}
