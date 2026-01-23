using LocaGuest.Application.DTOs.Rentability;

namespace LocaGuest.Application.Services;

public interface IRentabilityEngine
{
    RentabilityComputeOutput Compute(RentabilityInputDto input, string? clientCalcVersion = null);
}

public sealed record RentabilityComputeOutput(
    RentabilityResultDto Result,
    IReadOnlyList<string> Warnings,
    string InputsHash,
    string CalculationVersion,
    string ResultsJson
);
