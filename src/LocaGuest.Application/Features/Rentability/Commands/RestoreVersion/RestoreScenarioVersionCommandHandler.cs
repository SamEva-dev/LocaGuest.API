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

namespace LocaGuest.Application.Features.Rentability.Commands.RestoreVersion;

public class RestoreScenarioVersionCommandHandler : IRequestHandler<RestoreScenarioVersionCommand, Result<RentabilityScenarioDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocaGuestDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RestoreScenarioVersionCommandHandler> _logger;

    public RestoreScenarioVersionCommandHandler(
        IUnitOfWork unitOfWork,
        ILocaGuestDbContext context,
        ICurrentUserService currentUserService,
        ILogger<RestoreScenarioVersionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<RentabilityScenarioDto>> Handle(RestoreScenarioVersionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId);

            var scenario = await _context.RentabilityScenarios
                .FirstOrDefaultAsync(s => s.Id == request.ScenarioId && s.UserId == userId, cancellationToken);

            if (scenario == null)
            {
                return Result.Failure<RentabilityScenarioDto>("Scenario not found");
            }

            var version = await _context.ScenarioVersions
                .FirstOrDefaultAsync(v => v.Id == request.VersionId && v.ScenarioId == request.ScenarioId, cancellationToken);

            if (version == null)
            {
                return Result.Failure<RentabilityScenarioDto>("Version not found");
            }

            // Parse snapshot and restore
            var snapshot = JsonSerializer.Deserialize<ScenarioSnapshot>(version.SnapshotJson);
            if (snapshot == null)
            {
                return Result.Failure<RentabilityScenarioDto>("Invalid snapshot data");
            }

            // Create new version before restoring (backup current state)
            var currentSnapshot = CreateSnapshot(scenario);
            var currentSnapshotJson = JsonSerializer.Serialize(currentSnapshot);
            scenario.CreateVersion($"Backup before restoring v{version.VersionNumber}", currentSnapshotJson);

            // Restore data
            RestoreFromSnapshot(scenario, snapshot);

            await _unitOfWork.CommitAsync(cancellationToken);

            var dto = MapToDto(scenario);
            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring scenario version {VersionId}", request.VersionId);
            return Result.Failure<RentabilityScenarioDto>("Error restoring version");
        }
    }

    private ScenarioSnapshot CreateSnapshot(Domain.Aggregates.RentabilityAggregate.RentabilityScenario scenario)
    {
        return new ScenarioSnapshot
        {
            PropertyType = scenario.PropertyType,
            Location = scenario.Location,
            Surface = scenario.Surface,
            State = scenario.State,
            Strategy = scenario.Strategy,
            Horizon = scenario.Horizon,
            Objective = scenario.Objective,
            PurchasePrice = scenario.PurchasePrice,
            NotaryFees = scenario.NotaryFees,
            RenovationCost = scenario.RenovationCost,
            LandValue = scenario.LandValue ?? 0,
            FurnitureCost = scenario.FurnitureCost ?? 0,
            MonthlyRent = scenario.MonthlyRent,
            Indexation = scenario.Indexation,
            IndexationRate = scenario.IndexationRate,
            VacancyRate = scenario.VacancyRate,
            SeasonalityEnabled = scenario.SeasonalityEnabled,
            HighSeasonMultiplier = scenario.HighSeasonMultiplier ?? 0,
            ParkingRent = scenario.ParkingRent ?? 0,
            StorageRent = scenario.StorageRent ?? 0,
            OtherRevenues = scenario.OtherRevenues ?? 0,
            GuaranteedRent = scenario.GuaranteedRent,
            RelocationIncrease = scenario.RelocationIncrease ?? 0,
            CondoFees = scenario.CondoFees,
            Insurance = scenario.Insurance,
            PropertyTax = scenario.PropertyTax,
            ManagementFees = scenario.ManagementFees,
            MaintenanceRate = scenario.MaintenanceRate,
            RecoverableCharges = scenario.RecoverableCharges,
            ChargesIncrease = scenario.ChargesIncrease,
            PlannedCapexJson = scenario.PlannedCapexJson,
            LoanAmount = scenario.LoanAmount,
            LoanType = scenario.LoanType,
            InterestRate = scenario.InterestRate,
            Duration = scenario.Duration,
            InsuranceRate = scenario.InsuranceRate,
            DeferredMonths = scenario.DeferredMonths,
            DeferredType = scenario.DeferredType,
            EarlyRepaymentPenalty = scenario.EarlyRepaymentPenalty,
            IncludeNotaryInLoan = scenario.IncludeNotaryInLoan,
            IncludeRenovationInLoan = scenario.IncludeRenovationInLoan,
            TaxRegime = scenario.TaxRegime,
            MarginalTaxRate = scenario.MarginalTaxRate,
            SocialContributions = scenario.SocialContributions,
            DepreciationYears = scenario.DepreciationYears,
            FurnitureDepreciationYears = scenario.FurnitureDepreciationYears,
            DeficitCarryForward = scenario.DeficitCarryForward,
            CrlApplicable = scenario.CrlApplicable,
            ExitMethod = scenario.ExitMethod,
            TargetCapRate = scenario.TargetCapRate,
            AnnualAppreciation = scenario.AnnualAppreciation,
            TargetPricePerSqm = scenario.TargetPricePerSqm,
            SellingCosts = scenario.SellingCosts,
            CapitalGainsTax = scenario.CapitalGainsTax,
            HoldYears = scenario.HoldYears,
            ResultsJson = scenario.ResultsJson
        };
    }

    private void RestoreFromSnapshot(Domain.Aggregates.RentabilityAggregate.RentabilityScenario scenario, ScenarioSnapshot snapshot)
    {
        scenario.UpdateContext(
            snapshot.PropertyType, snapshot.Location, snapshot.Surface, snapshot.State,
            snapshot.Strategy, snapshot.Horizon, snapshot.Objective, snapshot.PurchasePrice,
            snapshot.NotaryFees, snapshot.RenovationCost, snapshot.LandValue, snapshot.FurnitureCost
        );

        scenario.UpdateRevenues(
            snapshot.MonthlyRent, snapshot.Indexation, snapshot.IndexationRate, snapshot.VacancyRate,
            snapshot.SeasonalityEnabled, snapshot.HighSeasonMultiplier, snapshot.ParkingRent,
            snapshot.StorageRent, snapshot.OtherRevenues, snapshot.GuaranteedRent, snapshot.RelocationIncrease
        );

        scenario.UpdateCharges(
            snapshot.CondoFees, snapshot.Insurance, snapshot.PropertyTax, snapshot.ManagementFees,
            snapshot.MaintenanceRate, snapshot.RecoverableCharges, snapshot.ChargesIncrease,
            snapshot.PlannedCapexJson
        );

        scenario.UpdateFinancing(
            snapshot.LoanAmount, snapshot.LoanType, snapshot.InterestRate, snapshot.Duration,
            snapshot.InsuranceRate, snapshot.DeferredMonths, snapshot.DeferredType,
            snapshot.EarlyRepaymentPenalty, snapshot.IncludeNotaryInLoan, snapshot.IncludeRenovationInLoan
        );

        scenario.UpdateTax(
            snapshot.TaxRegime, snapshot.MarginalTaxRate, snapshot.SocialContributions,
            snapshot.DepreciationYears ?? 0, snapshot.FurnitureDepreciationYears ?? 0,
            snapshot.DeficitCarryForward, snapshot.CrlApplicable
        );

        scenario.UpdateExit(
            snapshot.ExitMethod, snapshot.TargetCapRate ?? 0, snapshot.AnnualAppreciation ?? 0,
            snapshot.TargetPricePerSqm ?? 0, snapshot.SellingCosts, snapshot.CapitalGainsTax, snapshot.HoldYears
        );

        if (!string.IsNullOrEmpty(snapshot.ResultsJson))
        {
            scenario.UpdateResults(snapshot.ResultsJson);
        }
    }

    private RentabilityScenarioDto MapToDto(Domain.Aggregates.RentabilityAggregate.RentabilityScenario scenario)
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

    private class ScenarioSnapshot
    {
        public string PropertyType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal Surface { get; set; }
        public string State { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public int Horizon { get; set; }
        public string Objective { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public decimal NotaryFees { get; set; }
        public decimal RenovationCost { get; set; }
        public decimal LandValue { get; set; }
        public decimal FurnitureCost { get; set; }
        public decimal MonthlyRent { get; set; }
        public string Indexation { get; set; } = string.Empty;
        public decimal IndexationRate { get; set; }
        public decimal VacancyRate { get; set; }
        public bool SeasonalityEnabled { get; set; }
        public decimal HighSeasonMultiplier { get; set; }
        public decimal ParkingRent { get; set; }
        public decimal StorageRent { get; set; }
        public decimal OtherRevenues { get; set; }
        public bool GuaranteedRent { get; set; }
        public decimal RelocationIncrease { get; set; }
        public decimal CondoFees { get; set; }
        public decimal Insurance { get; set; }
        public decimal PropertyTax { get; set; }
        public decimal ManagementFees { get; set; }
        public decimal MaintenanceRate { get; set; }
        public decimal RecoverableCharges { get; set; }
        public decimal ChargesIncrease { get; set; }
        public string? PlannedCapexJson { get; set; }
        public decimal LoanAmount { get; set; }
        public string LoanType { get; set; } = string.Empty;
        public decimal InterestRate { get; set; }
        public int Duration { get; set; }
        public decimal InsuranceRate { get; set; }
        public int DeferredMonths { get; set; }
        public string DeferredType { get; set; } = string.Empty;
        public decimal EarlyRepaymentPenalty { get; set; }
        public bool IncludeNotaryInLoan { get; set; }
        public bool IncludeRenovationInLoan { get; set; }
        public string TaxRegime { get; set; } = string.Empty;
        public decimal MarginalTaxRate { get; set; }
        public decimal SocialContributions { get; set; }
        public int? DepreciationYears { get; set; }
        public int? FurnitureDepreciationYears { get; set; }
        public bool DeficitCarryForward { get; set; }
        public bool CrlApplicable { get; set; }
        public string ExitMethod { get; set; } = string.Empty;
        public decimal? TargetCapRate { get; set; }
        public decimal? AnnualAppreciation { get; set; }
        public decimal? TargetPricePerSqm { get; set; }
        public decimal SellingCosts { get; set; }
        public decimal CapitalGainsTax { get; set; }
        public int HoldYears { get; set; }
        public string? ResultsJson { get; set; }
    }
}
