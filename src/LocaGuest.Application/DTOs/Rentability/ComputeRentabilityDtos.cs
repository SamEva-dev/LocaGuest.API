using System.Text.Json.Serialization;

namespace LocaGuest.Application.DTOs.Rentability;

public sealed record ComputeRentabilityRequest(
    RentabilityInputDto Inputs,
    string? ClientCalcVersion
);

public sealed record ComputeRentabilityResponse(
    RentabilityResultDto Results,
    IReadOnlyList<string> Warnings,
    string CalculationVersion,
    string InputsHash,
    bool IsCertified
);

public sealed record RentabilityResultDto
{
    public IReadOnlyList<RentabilityYearlyResultDto> YearlyResults { get; init; } = Array.Empty<RentabilityYearlyResultDto>();
    public RentabilityKpisDto Kpis { get; init; } = new();

    // cashflows incl t0
    public IReadOnlyList<decimal> Cashflows { get; init; } = Array.Empty<decimal>();

    public RentabilityMetadataDto? Metadata { get; init; }
}

public sealed record RentabilityMetadataDto
{
    public string CalculationVersion { get; init; } = string.Empty;
}

public sealed record RentabilityYearlyResultDto
{
    public int Year { get; init; }

    public decimal GrossRevenue { get; init; }
    public decimal VacancyLoss { get; init; }
    public decimal NetRevenue { get; init; }

    public decimal CondoFees { get; init; }
    public decimal Insurance { get; init; }
    public decimal PropertyTax { get; init; }
    public decimal Management { get; init; }
    public decimal Maintenance { get; init; }
    public decimal Capex { get; init; }
    public decimal RecoverableCharges { get; init; }
    public decimal TotalCharges { get; init; }

    public decimal LoanPayment { get; init; }
    public decimal Interest { get; init; }
    public decimal Principal { get; init; }
    public decimal LoanInsurance { get; init; }
    public decimal RemainingDebt { get; init; }

    public decimal Noi { get; init; }

    public decimal TaxableIncome { get; init; }
    public decimal Depreciation { get; init; }
    public decimal Tax { get; init; }

    public decimal CashflowBeforeTax { get; init; }
    public decimal CashflowAfterTax { get; init; }

    public decimal PropertyValue { get; init; }
}

public sealed record RentabilityKpisDto
{
    public decimal TotalInvestment { get; init; }
    public decimal OwnFunds { get; init; }

    public decimal GrossYield { get; init; }
    public decimal NetYield { get; init; }
    public decimal NetNetYield { get; init; }

    public decimal Dscr { get; init; }
    public decimal PaybackYears { get; init; }

    public decimal Irr { get; init; }
    public decimal Npv { get; init; }
    public decimal TotalReturn { get; init; }
}
