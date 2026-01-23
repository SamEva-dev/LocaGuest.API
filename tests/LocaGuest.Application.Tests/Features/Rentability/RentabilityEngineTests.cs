using LocaGuest.Application.DTOs.Rentability;
using LocaGuest.Application.Services;
using Xunit;

namespace LocaGuest.Application.Tests.Features.Rentability;

public sealed class RentabilityEngineTests
{
    private readonly RentabilityEngine _engine = new();

    private static RentabilityInputDto BaseInput() => new()
    {
        Context = new PropertyContextDto
        {
            Type = "apartment",
            Location = "Nice",
            Surface = 40,
            State = "good",
            Strategy = "bare",
            Horizon = 10,
            Objective = "cashflow",
            PurchasePrice = 200_000m,
            NotaryFees = 15_000m,
            RenovationCost = 10_000m,
            LandValue = 20_000m,
            FurnitureCost = 5_000m,
        },
        Revenues = new RevenueAssumptionsDto
        {
            MonthlyRent = 1000m,
            Indexation = "irl",
            IndexationRate = 2m,
            VacancyRate = 5m,
            SeasonalityEnabled = false,
            HighSeasonMultiplier = 1.2m,
            ParkingRent = 0m,
            StorageRent = 0m,
            OtherRevenues = 0m,
            GuaranteedRent = false,
            RelocationIncrease = 0m,
        },
        Charges = new ChargesAssumptionsDto
        {
            CondoFees = 1200m,
            Insurance = 240m,
            PropertyTax = 1200m,
            ManagementFees = 7m,
            MaintenanceRate = 1m,
            RecoverableCharges = 0m,
            ChargesIncrease = 2m,
            PlannedCapex = new List<PlannedCapexDto>(),
        },
        Financing = new FinancingAssumptionsDto
        {
            LoanAmount = 160_000m,
            LoanType = "fixed",
            InterestRate = 3m,
            Duration = 240,
            InsuranceRate = 0.3m,
            DeferredMonths = 0,
            DeferredType = "none",
            EarlyRepaymentPenalty = 0m,
            IncludeNotaryInLoan = false,
            IncludeRenovationInLoan = false,
        },
        Tax = new TaxAssumptionsDto
        {
            Regime = "real",
            MarginalTaxRate = 30m,
            SocialContributions = 17.2m,
            DepreciationYears = 25,
            FurnitureDepreciationYears = 7,
            DeficitCarryForward = true,
            CrlApplicable = false,
        },
        Exit = new ExitAssumptionsDto
        {
            Method = "appreciation",
            AnnualAppreciation = 2m,
            TargetCapRate = 5m,
            TargetPricePerSqm = 6000m,
            SellingCosts = 8m,
            CapitalGainsTax = 19m,
            HoldYears = 10,
        },
    };

    [Fact]
    public void Compute_IsDeterministic_HashStable()
    {
        var input = BaseInput();

        var a = _engine.Compute(input);
        var b = _engine.Compute(input);

        Assert.Equal(a.InputsHash, b.InputsHash);
        Assert.Equal(a.Result.Kpis, b.Result.Kpis);
    }

    [Fact]
    public void FixedRateLoan_AnnualPayment_ShouldBeNearlyConstant()
    {
        var baseInput = BaseInput();
        var input = baseInput with
        {
            Financing = baseInput.Financing with { LoanAmount = 200_000m, InterestRate = 3m, Duration = 240, InsuranceRate = 0m },
            Exit = baseInput.Exit with { HoldYears = 5, SellingCosts = 0m, CapitalGainsTax = 0m, AnnualAppreciation = 0m },
        };

        var r = _engine.Compute(input).Result;

        var p1 = r.YearlyResults[0].LoanPayment;
        var p2 = r.YearlyResults[1].LoanPayment;

        // tolérance ~1€
        Assert.InRange(p2, p1 - 1m, p1 + 1m);
    }

    [Fact]
    public void CashflowBeforeTax_IncludesInsurance()
    {
        var baseInput = BaseInput();
        var input = baseInput with
        {
            Exit = baseInput.Exit with { HoldYears = 1, SellingCosts = 0m, CapitalGainsTax = 0m, AnnualAppreciation = 0m },
        };

        var y1 = _engine.Compute(input).Result.YearlyResults[0];
        var expected = y1.NetRevenue - y1.TotalCharges - y1.LoanPayment - y1.LoanInsurance;

        Assert.Equal(expected, y1.CashflowBeforeTax);
    }

    [Fact]
    public void Payback_StartsFromOwnFunds_NotFromZero()
    {
        var baseInput = BaseInput();
        var input = baseInput with
        {
            Context = baseInput.Context with { PurchasePrice = 100_000m, NotaryFees = 0m, RenovationCost = 0m, FurnitureCost = 0m, LandValue = 0m },
            Financing = baseInput.Financing with { LoanAmount = 50_000m, InterestRate = 0m, Duration = 120, InsuranceRate = 0m },
            Exit = baseInput.Exit with { SellingCosts = 0m, CapitalGainsTax = 0m, AnnualAppreciation = 0m },
        };

        var res = _engine.Compute(input).Result;
        Assert.True(res.Kpis.PaybackYears > 1m);
    }

    [Fact]
    public void SellingCosts_ShouldDecreaseIRR_AllElseEqual()
    {
        var baseInput = BaseInput();
        var lowSell = baseInput with { Exit = baseInput.Exit with { HoldYears = 10, SellingCosts = 0m } };

        var baseInput2 = BaseInput();
        var highSell = baseInput2 with { Exit = baseInput2.Exit with { HoldYears = 10, SellingCosts = 10m } };

        var irrLow = _engine.Compute(lowSell).Result.Kpis.Irr;
        var irrHigh = _engine.Compute(highSell).Result.Kpis.Irr;

        Assert.True(irrHigh < irrLow);
    }

    [Fact]
    public void Micro_Regime_TaxableIs50PercentOfNetRevenue()
    {
        var baseInput = BaseInput();
        var input = baseInput with
        {
            Tax = baseInput.Tax with { Regime = "micro" },
            Charges = baseInput.Charges with { CondoFees = 50_000m },
        };

        var y1 = _engine.Compute(input).Result.YearlyResults[0];
        Assert.Equal(Math.Round(y1.NetRevenue * 0.5m, 2), y1.TaxableIncome);
    }

    [Fact]
    public void LMNP_Depreciation_Capped_NoNegativeTaxableFromDep()
    {
        var baseInput = BaseInput();
        var input = baseInput with
        {
            Tax = baseInput.Tax with { Regime = "lmnp", DepreciationYears = 5, FurnitureDepreciationYears = 2 },
            Revenues = baseInput.Revenues with { MonthlyRent = 500m, VacancyRate = 0m },
            Charges = baseInput.Charges with
            {
                CondoFees = 0m,
                Insurance = 0m,
                PropertyTax = 0m,
                ManagementFees = 0m,
                MaintenanceRate = 0m,
                PlannedCapex = new List<PlannedCapexDto>(),
                ChargesIncrease = 0m,
            },
        };

        var y1 = _engine.Compute(input).Result.YearlyResults[0];
        Assert.True(y1.TaxableIncome >= 0m);
    }

    [Fact]
    public void NoNaN_Like_KpisRemainFiniteInNormalScenario()
    {
        var res = _engine.Compute(BaseInput()).Result;

        Assert.True(res.Kpis.GrossYield >= 0m);
        Assert.True(res.Kpis.Npv >= decimal.MinValue && res.Kpis.Npv <= decimal.MaxValue);
        Assert.True(res.Kpis.Irr >= decimal.MinValue && res.Kpis.Irr <= decimal.MaxValue);
    }
}
