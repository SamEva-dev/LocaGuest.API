using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Rentability;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LocaGuest.Application.Features.Rentability.Queries.GetUserScenarios;

public class GetUserScenariosQueryHandler : IRequestHandler<GetUserScenariosQuery, Result<List<RentabilityScenarioDto>>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUserScenariosQueryHandler> _logger;

    public GetUserScenariosQueryHandler(
        ILocaGuestDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetUserScenariosQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<List<RentabilityScenarioDto>>> Handle(GetUserScenariosQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId);

            var scenarios = await _context.RentabilityScenarios
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.IsBase)
                .ThenByDescending(s => s.LastModifiedAt)
                .ToListAsync(cancellationToken);

            var dtos = scenarios.Select(scenario =>
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
            }).ToList();

            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user rentability scenarios");
            return Result.Failure<List<RentabilityScenarioDto>>("Error fetching scenarios");
        }
    }
}
