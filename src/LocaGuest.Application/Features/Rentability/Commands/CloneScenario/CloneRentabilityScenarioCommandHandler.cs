using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Rentability;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.RentabilityAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LocaGuest.Application.Features.Rentability.Commands.CloneScenario;

public class CloneRentabilityScenarioCommandHandler : IRequestHandler<CloneRentabilityScenarioCommand, Result<RentabilityScenarioDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocaGuestDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CloneRentabilityScenarioCommandHandler> _logger;

    public CloneRentabilityScenarioCommandHandler(
        IUnitOfWork unitOfWork,
        ILocaGuestDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CloneRentabilityScenarioCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<RentabilityScenarioDto>> Handle(CloneRentabilityScenarioCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId);

            var source = await _context.RentabilityScenarios
                .FirstOrDefaultAsync(s => s.Id == request.SourceId && s.UserId == userId, cancellationToken);

            if (source == null)
            {
                return Result.Failure<RentabilityScenarioDto>("Source scenario not found");
            }

            // Create new scenario with same data
            var clone = RentabilityScenario.Create(userId, request.NewName, false);

            clone.UpdateContext(
                source.PropertyType, source.Location, source.Surface, source.State,
                source.Strategy, source.Horizon, source.Objective, source.PurchasePrice,
                source.NotaryFees, source.RenovationCost, source.LandValue, source.FurnitureCost
            );

            clone.UpdateRevenues(
                source.MonthlyRent, source.Indexation, source.IndexationRate, source.VacancyRate,
                source.SeasonalityEnabled, source.HighSeasonMultiplier, source.ParkingRent,
                source.StorageRent, source.OtherRevenues, source.GuaranteedRent, source.RelocationIncrease
            );

            clone.UpdateCharges(
                source.CondoFees, source.Insurance, source.PropertyTax, source.ManagementFees,
                source.MaintenanceRate, source.RecoverableCharges, source.ChargesIncrease,
                source.PlannedCapexJson
            );

            clone.UpdateFinancing(
                source.LoanAmount, source.LoanType, source.InterestRate, source.Duration,
                source.InsuranceRate, source.DeferredMonths, source.DeferredType,
                source.EarlyRepaymentPenalty, source.IncludeNotaryInLoan, source.IncludeRenovationInLoan
            );

            clone.UpdateTax(
                source.TaxRegime, source.MarginalTaxRate, source.SocialContributions,
                source.DepreciationYears, source.FurnitureDepreciationYears,
                source.DeficitCarryForward, source.CrlApplicable
            );

            clone.UpdateExit(
                source.ExitMethod, source.TargetCapRate, source.AnnualAppreciation,
                source.TargetPricePerSqm, source.SellingCosts, source.CapitalGainsTax, source.HoldYears
            );

            if (!string.IsNullOrEmpty(source.ResultsJson))
            {
                clone.UpdateResults(source.ResultsJson);
            }

            _context.RentabilityScenarios.Add(clone);
            await _unitOfWork.CommitAsync(cancellationToken);

            var dto = MapToDto(clone);
            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning rentability scenario {SourceId}", request.SourceId);
            return Result.Failure<RentabilityScenarioDto>("Error cloning scenario");
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
