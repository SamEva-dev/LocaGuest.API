using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Rentability;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.RentabilityAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LocaGuest.Application.Features.Rentability.Commands.SaveScenario;

public class SaveRentabilityScenarioCommandHandler : IRequestHandler<SaveRentabilityScenarioCommand, Result<RentabilityScenarioDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SaveRentabilityScenarioCommandHandler> _logger;

    public SaveRentabilityScenarioCommandHandler(
        ILocaGuestDbContext context,
        ICurrentUserService currentUserService,
        ILogger<SaveRentabilityScenarioCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<RentabilityScenarioDto>> Handle(SaveRentabilityScenarioCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId);
            RentabilityScenario scenario;

            if (request.Id.HasValue)
            {
                // Update existing scenario
                scenario = await _context.RentabilityScenarios
                    .FirstOrDefaultAsync(s => s.Id == request.Id.Value && s.UserId == userId, cancellationToken);

                if (scenario == null)
                {
                    return Result.Failure<RentabilityScenarioDto>("Scenario not found");
                }

                scenario.UpdateName(request.Name);
            }
            else
            {
                // Create new scenario
                scenario = RentabilityScenario.Create(userId, request.Name, request.IsBase);
                _context.RentabilityScenarios.Add(scenario);
            }

            // Update all sections
            var input = request.Input;

            scenario.UpdateContext(
                input.Context.Type,
                input.Context.Location,
                input.Context.Surface,
                input.Context.State,
                input.Context.Strategy,
                input.Context.Horizon,
                input.Context.Objective,
                input.Context.PurchasePrice,
                input.Context.NotaryFees,
                input.Context.RenovationCost,
                input.Context.LandValue,
                input.Context.FurnitureCost
            );

            scenario.UpdateRevenues(
                input.Revenues.MonthlyRent,
                input.Revenues.Indexation,
                input.Revenues.IndexationRate,
                input.Revenues.VacancyRate,
                input.Revenues.SeasonalityEnabled,
                input.Revenues.HighSeasonMultiplier,
                input.Revenues.ParkingRent,
                input.Revenues.StorageRent,
                input.Revenues.OtherRevenues,
                input.Revenues.GuaranteedRent,
                input.Revenues.RelocationIncrease
            );

            var capexJson = input.Charges.PlannedCapex != null
                ? JsonSerializer.Serialize(input.Charges.PlannedCapex)
                : null;

            scenario.UpdateCharges(
                input.Charges.CondoFees,
                input.Charges.Insurance,
                input.Charges.PropertyTax,
                input.Charges.ManagementFees,
                input.Charges.MaintenanceRate,
                input.Charges.RecoverableCharges,
                input.Charges.ChargesIncrease,
                capexJson
            );

            scenario.UpdateFinancing(
                input.Financing.LoanAmount,
                input.Financing.LoanType,
                input.Financing.InterestRate,
                input.Financing.Duration,
                input.Financing.InsuranceRate,
                input.Financing.DeferredMonths,
                input.Financing.DeferredType,
                input.Financing.EarlyRepaymentPenalty,
                input.Financing.IncludeNotaryInLoan,
                input.Financing.IncludeRenovationInLoan
            );

            scenario.UpdateTax(
                input.Tax.Regime,
                input.Tax.MarginalTaxRate,
                input.Tax.SocialContributions,
                input.Tax.DepreciationYears,
                input.Tax.FurnitureDepreciationYears,
                input.Tax.DeficitCarryForward,
                input.Tax.CrlApplicable
            );

            scenario.UpdateExit(
                input.Exit.Method,
                input.Exit.TargetCapRate,
                input.Exit.AnnualAppreciation,
                input.Exit.TargetPricePerSqm,
                input.Exit.SellingCosts,
                input.Exit.CapitalGainsTax,
                input.Exit.HoldYears
            );

            if (request.ResultsJson != null)
            {
                scenario.UpdateResults(request.ResultsJson);
            }

            // If this is set as base, unset others
            if (request.IsBase)
            {
                var otherBaseScenarios = await _context.RentabilityScenarios
                    .Where(s => s.UserId == userId && s.IsBase && s.Id != scenario.Id)
                    .ToListAsync(cancellationToken);

                foreach (var other in otherBaseScenarios)
                {
                    other.UnsetAsBase();
                }

                scenario.SetAsBase();
            }

            await _context.SaveChangesAsync(cancellationToken);

            var dto = MapToDto(scenario);
            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving rentability scenario");
            return Result.Failure<RentabilityScenarioDto>("Error saving scenario");
        }
    }

    private RentabilityScenarioDto MapToDto(RentabilityScenario scenario)
    {
        var plannedCapex = !string.IsNullOrEmpty(scenario.PlannedCapexJson)
            ? JsonSerializer.Deserialize<List<PlannedCapexDto>>(scenario.PlannedCapexJson)
            : null;

        return new RentabilityScenarioDto
        {
            Id = scenario.Id,
            Name = scenario.Name,
            IsBase = scenario.IsBase,
            CreatedAt = scenario.CreatedAt,
            LastModifiedAt = scenario.LastModifiedAt,
            ResultsJson = scenario.ResultsJson,
            Input = new RentabilityInputDto
            {
                Context = new PropertyContextDto
                {
                    Type = scenario.PropertyType,
                    Location = scenario.Location,
                    Surface = scenario.Surface,
                    State = scenario.State,
                    Strategy = scenario.Strategy,
                    Horizon = scenario.Horizon,
                    Objective = scenario.Objective,
                    PurchasePrice = scenario.PurchasePrice,
                    NotaryFees = scenario.NotaryFees,
                    RenovationCost = scenario.RenovationCost,
                    LandValue = scenario.LandValue,
                    FurnitureCost = scenario.FurnitureCost
                },
                Revenues = new RevenueAssumptionsDto
                {
                    MonthlyRent = scenario.MonthlyRent,
                    Indexation = scenario.Indexation,
                    IndexationRate = scenario.IndexationRate,
                    VacancyRate = scenario.VacancyRate,
                    SeasonalityEnabled = scenario.SeasonalityEnabled,
                    HighSeasonMultiplier = scenario.HighSeasonMultiplier,
                    ParkingRent = scenario.ParkingRent,
                    StorageRent = scenario.StorageRent,
                    OtherRevenues = scenario.OtherRevenues,
                    GuaranteedRent = scenario.GuaranteedRent,
                    RelocationIncrease = scenario.RelocationIncrease
                },
                Charges = new ChargesAssumptionsDto
                {
                    CondoFees = scenario.CondoFees,
                    Insurance = scenario.Insurance,
                    PropertyTax = scenario.PropertyTax,
                    ManagementFees = scenario.ManagementFees,
                    MaintenanceRate = scenario.MaintenanceRate,
                    RecoverableCharges = scenario.RecoverableCharges,
                    ChargesIncrease = scenario.ChargesIncrease,
                    PlannedCapex = plannedCapex
                },
                Financing = new FinancingAssumptionsDto
                {
                    LoanAmount = scenario.LoanAmount,
                    LoanType = scenario.LoanType,
                    InterestRate = scenario.InterestRate,
                    Duration = scenario.Duration,
                    InsuranceRate = scenario.InsuranceRate,
                    DeferredMonths = scenario.DeferredMonths,
                    DeferredType = scenario.DeferredType,
                    EarlyRepaymentPenalty = scenario.EarlyRepaymentPenalty,
                    IncludeNotaryInLoan = scenario.IncludeNotaryInLoan,
                    IncludeRenovationInLoan = scenario.IncludeRenovationInLoan
                },
                Tax = new TaxAssumptionsDto
                {
                    Regime = scenario.TaxRegime,
                    MarginalTaxRate = scenario.MarginalTaxRate,
                    SocialContributions = scenario.SocialContributions,
                    DepreciationYears = scenario.DepreciationYears,
                    FurnitureDepreciationYears = scenario.FurnitureDepreciationYears,
                    DeficitCarryForward = scenario.DeficitCarryForward,
                    CrlApplicable = scenario.CrlApplicable
                },
                Exit = new ExitAssumptionsDto
                {
                    Method = scenario.ExitMethod,
                    TargetCapRate = scenario.TargetCapRate,
                    AnnualAppreciation = scenario.AnnualAppreciation,
                    TargetPricePerSqm = scenario.TargetPricePerSqm,
                    SellingCosts = scenario.SellingCosts,
                    CapitalGainsTax = scenario.CapitalGainsTax,
                    HoldYears = scenario.HoldYears
                }
            }
        };
    }
}
