using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocaGuest.Application.DTOs.Rentability;

namespace LocaGuest.Application.Services;

public sealed class RentabilityEngine : IRentabilityEngine
{
    public const string CalcVersion = "server-1.1.0";

    private const decimal DefaultDiscountRate = 0.06m; // 6%

    public RentabilityComputeOutput Compute(RentabilityInputDto input, string? clientCalcVersion = null)
    {
        var warnings = new List<string>();
        ValidateAndWarn(input, warnings);

        if (!string.IsNullOrWhiteSpace(clientCalcVersion) && !string.Equals(clientCalcVersion, "front-1.1.0", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"ClientCalcVersion={clientCalcVersion} differs from expected front-1.1.0");
        }

        var inputsHash = "sha256:" + ComputeCanonicalSha256(input);

        var holdYears = ClampInt(input.Exit.HoldYears > 0 ? input.Exit.HoldYears : input.Context.Horizon, 1, 60);

        var purchasePrice = Money(input.Context.PurchasePrice);
        var notaryFees = Money(input.Context.NotaryFees);
        var renovationCost = Money(input.Context.RenovationCost);
        var furnitureCost = Money(input.Context.FurnitureCost ?? 0);

        var totalInvestment = Money(purchasePrice + notaryFees + renovationCost + furnitureCost);

        var loanAmount = Money(input.Financing.LoanAmount);
        var ownFunds = Money(Math.Max(0, totalInvestment - loanAmount));

        var monthlyRent = Money(input.Revenues.MonthlyRent);
        var otherMonthly = Money((input.Revenues.ParkingRent ?? 0) + (input.Revenues.StorageRent ?? 0) + (input.Revenues.OtherRevenues ?? 0));
        var vacancyRate = ClampPct(input.Revenues.VacancyRate);
        var indexationRate = ClampPct(input.Revenues.IndexationRate);

        // Charges
        var condoFees0 = Money(input.Charges.CondoFees) * 12m;
        var insurance0 = Money(input.Charges.Insurance) * 12m;
        var propertyTax0 = Money(input.Charges.PropertyTax);

        var managementPct = ClampPct(input.Charges.ManagementFees);
        var maintenancePct = ClampPct(input.Charges.MaintenanceRate);
        var chargesIncrease = ClampPct(input.Charges.ChargesIncrease);
        var recoverableChargesMonthly = Money(input.Charges.RecoverableCharges);

        var capexByYear = (input.Charges.PlannedCapex ?? new List<PlannedCapexDto>())
            .GroupBy(x => ClampInt(x.Year, 1, holdYears))
            .ToDictionary(g => g.Key, g => Money(g.Sum(x => x.Amount)));

        var loan = ComputeLoanSchedule(
            principal: loanAmount,
            annualRatePct: ClampPct(input.Financing.InterestRate),
            totalMonths: ClampInt(input.Financing.Duration, 0, 1200),
            annualInsuranceRatePct: ClampPct(input.Financing.InsuranceRate),
            yearsToCompute: holdYears);

        // Tax
        var regime = (input.Tax.Regime ?? "real").ToLowerInvariant();
        var marginalTaxRate = ClampPct(input.Tax.MarginalTaxRate);
        var socialContrib = ClampPct(input.Tax.SocialContributions);
        var depreciationYears = ClampInt(input.Tax.DepreciationYears ?? 25, 1, 60);
        var furnitureDepYears = ClampInt(input.Tax.FurnitureDepreciationYears ?? 7, 1, 60);
        var deficitCarryForward = input.Tax.DeficitCarryForward;

        var landValue = Money(input.Context.LandValue ?? 0);
        var buildingBase = Money(Math.Max(0, purchasePrice - landValue));
        var buildingDepAnnual = depreciationYears > 0 ? Money(buildingBase / depreciationYears) : 0;
        var furnitureDepAnnual = furnitureDepYears > 0 ? Money(furnitureCost / furnitureDepYears) : 0;

        // Exit
        var appreciation = ClampPct(input.Exit.AnnualAppreciation ?? 0);
        var sellingCostsPct = ClampPct(input.Exit.SellingCosts);
        var capitalGainsTaxPct = ClampPct(input.Exit.CapitalGainsTax);

        var yearly = new List<RentabilityYearlyResultDto>(holdYears);

        var cashflows = new List<decimal>(holdYears + 1) { Money(-ownFunds) };

        for (var y = 1; y <= holdYears; y++)
        {
            var indexFactor = Pow(1 + indexationRate / 100m, y - 1);

            var grossRevenue = Money((monthlyRent + otherMonthly) * 12m * indexFactor);
            var vacancyLoss = Money(grossRevenue * (vacancyRate / 100m));
            var netRevenue = Money(grossRevenue - vacancyLoss);

            var opexFactor = Pow(1 + chargesIncrease / 100m, y - 1);

            var condoFees = Money(condoFees0 * opexFactor);
            var insurance = Money(insurance0 * opexFactor);
            var propertyTax = Money(propertyTax0 * opexFactor);

            var management = Money(netRevenue * (managementPct / 100m));
            var maintenance = Money(netRevenue * (maintenancePct / 100m));

            var recoverableCharges = Money(recoverableChargesMonthly * 12m * opexFactor);
            var capex = capexByYear.TryGetValue(y, out var cx) ? cx : 0m;

            var totalCharges = Money(condoFees + insurance + propertyTax + management + maintenance + capex - recoverableCharges);

            var loanYear = loan.Yearly[y - 1];
            var loanPayment = Money(loanYear.Payment);
            var loanInsurance = Money(loanYear.Insurance);
            var loanInterest = Money(loanYear.Interest);
            var loanPrincipal = Money(loanYear.Principal);
            var remainingDebt = Money(loanYear.RemainingDebt);

            // NOI "opex" (exclude capex) for DSCR
            var noi = Money(netRevenue - (condoFees + insurance + propertyTax + management + maintenance - recoverableCharges));

            var cashflowBeforeTax = Money(netRevenue - totalCharges - loanPayment - loanInsurance);

            var taxableIncome = ComputeTaxableIncome(regime, netRevenue, totalCharges, loanInterest, buildingDepAnnual, furnitureDepAnnual, deficitCarryForward);

            var taxAmount = taxableIncome <= 0
                ? 0
                : Money(taxableIncome * ((marginalTaxRate + socialContrib) / 100m));

            var cashflowAfterTax = Money(cashflowBeforeTax - taxAmount);

            var propertyValue = Money(purchasePrice * Pow(1 + appreciation / 100m, y));

            yearly.Add(new RentabilityYearlyResultDto
            {
                Year = y,
                GrossRevenue = grossRevenue,
                VacancyLoss = vacancyLoss,
                NetRevenue = netRevenue,

                CondoFees = condoFees,
                Insurance = insurance,
                PropertyTax = propertyTax,
                Management = management,
                Maintenance = maintenance,
                Capex = capex,
                RecoverableCharges = recoverableCharges,
                TotalCharges = totalCharges,

                LoanPayment = loanPayment,
                LoanInsurance = loanInsurance,
                Interest = loanInterest,
                Principal = loanPrincipal,
                RemainingDebt = remainingDebt,

                Noi = noi,

                TaxableIncome = taxableIncome,
                Depreciation = regime == "lmnp" ? Money(buildingDepAnnual + furnitureDepAnnual) : 0,
                Tax = taxAmount,

                CashflowBeforeTax = cashflowBeforeTax,
                CashflowAfterTax = cashflowAfterTax,

                PropertyValue = propertyValue,
            });

            cashflows.Add(cashflowAfterTax);
        }

        // terminal cashflow (sale)
        var last = yearly[^1];
        var salePrice = Money(last.PropertyValue);
        var sellingCosts = Money(salePrice * (sellingCostsPct / 100m));
        var capitalGain = Money(Math.Max(0, salePrice - purchasePrice));
        var capitalGainsTax = Money(capitalGain * (capitalGainsTaxPct / 100m));
        var debtToRepay = Money(last.RemainingDebt);
        var terminalNet = Money(salePrice - sellingCosts - capitalGainsTax - debtToRepay);

        cashflows[^1] = Money(cashflows[^1] + terminalNet);

        // KPIs
        var year1 = yearly[0];

        var grossYield = totalInvestment > 0 ? Round((year1.GrossRevenue / totalInvestment) * 100m, 2) : 0;
        var netYield = totalInvestment > 0 ? Round((year1.NetRevenue / totalInvestment) * 100m, 2) : 0;
        var netNetYield = totalInvestment > 0
            ? Round(((year1.NetRevenue - year1.TotalCharges - year1.LoanInsurance) / totalInvestment) * 100m, 2)
            : 0;

        var debtService1 = Money(year1.LoanPayment + year1.LoanInsurance);
        var dscr = debtService1 > 0 ? Round(year1.Noi / debtService1, 2) : decimal.MaxValue;

        var paybackYears = ComputePaybackYears(ownFunds, yearly.Select(x => x.CashflowAfterTax).ToArray());

        var irr = Irr(cashflows.Select(x => (double)x).ToArray());
        var irrPct = double.IsFinite(irr) ? Round((decimal)(irr * 100.0), 2) : 0m;

        var npv = Money((decimal)Npv((double)DefaultDiscountRate, cashflows.Select(x => (double)x).ToArray()));

        var totalReturn = ownFunds > 0
            ? Round((((cashflows.Sum() + ownFunds) / ownFunds) * 100m) - 100m, 2)
            : 0;

        var kpis = new RentabilityKpisDto
        {
            TotalInvestment = totalInvestment,
            OwnFunds = ownFunds,
            GrossYield = grossYield,
            NetYield = netYield,
            NetNetYield = netNetYield,
            Dscr = dscr,
            PaybackYears = paybackYears,
            Irr = irrPct,
            Npv = npv,
            TotalReturn = totalReturn,
        };

        var result = new RentabilityResultDto
        {
            YearlyResults = yearly,
            Kpis = kpis,
            Cashflows = cashflows,
            Metadata = new RentabilityMetadataDto { CalculationVersion = CalcVersion },
        };

        var resultsJson = JsonSerializer.Serialize(result, JsonOptions);

        return new RentabilityComputeOutput(
            Result: result,
            Warnings: warnings,
            InputsHash: inputsHash,
            CalculationVersion: CalcVersion,
            ResultsJson: resultsJson);
    }

    private static void ValidateAndWarn(RentabilityInputDto input, List<string> warnings)
    {
        if (input.Revenues.VacancyRate > 40) warnings.Add("Vacance > 40% : scénario atypique.");

        if (input.Financing.Duration > 0 && input.Exit.HoldYears * 12 < input.Financing.Duration)
            warnings.Add("Horizon < durée du prêt : il restera du capital à rembourser à la revente.");

        if (input.Context.PurchasePrice <= 0) warnings.Add("PurchasePrice <= 0 : scénario invalide.");

        if (input.Exit.SellingCosts < 0) warnings.Add("SellingCosts négatif : ramené à 0.");

        var regime = (input.Tax.Regime ?? "real").ToLowerInvariant();
        if (regime is not ("micro" or "real" or "lmnp"))
            warnings.Add($"Régime fiscal '{input.Tax.Regime}' non supporté précisément : calcul approximatif (mode 'real').");
    }

    private static decimal ComputeTaxableIncome(
        string regime,
        decimal netRevenue,
        decimal totalCharges,
        decimal loanInterest,
        decimal buildingDepAnnual,
        decimal furnitureDepAnnual,
        bool deficitCarryForward)
    {
        if (regime == "micro")
            return Money(netRevenue * 0.5m);

        var taxable = Money(netRevenue - totalCharges - loanInterest);

        if (regime == "lmnp")
        {
            var amort = Money(buildingDepAnnual + furnitureDepAnnual);
            taxable = Money(Math.Max(0, taxable - amort));
            return taxable;
        }

        if (!deficitCarryForward)
            taxable = Money(Math.Max(0, taxable));

        return taxable;
    }

    private sealed record LoanYear(decimal Payment, decimal Insurance, decimal Interest, decimal Principal, decimal RemainingDebt);

    private sealed record LoanSchedule(IReadOnlyList<LoanYear> Yearly);

    private static LoanSchedule ComputeLoanSchedule(decimal principal, decimal annualRatePct, int totalMonths, decimal annualInsuranceRatePct, int yearsToCompute)
    {
        principal = Money(principal);

        if (principal <= 0 || totalMonths <= 0)
        {
            var zeros = Enumerable.Range(0, yearsToCompute).Select(_ => new LoanYear(0, 0, 0, 0, 0)).ToList();
            return new LoanSchedule(zeros);
        }

        var r = (annualRatePct / 100m) / 12m;
        var pmt = Money(MonthlyPayment(principal, r, totalMonths));

        var annualInsurance = Money(principal * (annualInsuranceRatePct / 100m));
        var monthlyInsurance = Money(annualInsurance / 12m);

        var yearly = Enumerable.Range(0, yearsToCompute).Select(_ => new LoanYear(0, 0, 0, 0, principal)).ToArray();

        var balance = principal;
        var monthsToCompute = Math.Min(totalMonths, yearsToCompute * 12);

        for (var m = 1; m <= monthsToCompute; m++)
        {
            var y = (m - 1) / 12;

            var interest = Money(balance * r);
            var principalPart = Money(Math.Min(pmt - interest, balance));
            balance = Money(balance - principalPart);

            var prev = yearly[y];
            yearly[y] = prev with
            {
                Payment = Money(prev.Payment + pmt),
                Insurance = Money(prev.Insurance + monthlyInsurance),
                Interest = Money(prev.Interest + interest),
                Principal = Money(prev.Principal + principalPart),
                RemainingDebt = balance
            };
        }

        for (var y = 0; y < yearsToCompute; y++)
        {
            if (y > 0 && yearly[y - 1].RemainingDebt <= 0.01m)
                yearly[y] = yearly[y] with { RemainingDebt = 0m };
        }

        return new LoanSchedule(yearly);
    }

    private static decimal MonthlyPayment(decimal principal, decimal rMonthly, int nMonths)
    {
        if (principal <= 0 || nMonths <= 0) return 0;
        if (Math.Abs((double)rMonthly) < 1e-12) return principal / nMonths;

        var pow = (decimal)Math.Pow((double)(1 + rMonthly), nMonths);
        return principal * (rMonthly * pow) / (pow - 1);
    }

    private static decimal ComputePaybackYears(decimal ownFunds, decimal[] yearlyCashflows)
    {
        if (ownFunds <= 0) return 0;

        decimal cum = -ownFunds;

        for (var i = 0; i < yearlyCashflows.Length; i++)
        {
            var cf = Money(yearlyCashflows[i]);
            var prev = cum;
            cum = Money(cum + cf);

            if (cum >= 0)
            {
                if (Math.Abs(cf) < 0.000000001m) return i + 1;
                var needed = -prev;
                var frac = needed / cf;
                return Round(i + frac, 2);
            }
        }

        return decimal.MaxValue; // infinity-like
    }

    private static double Npv(double rate, double[] cfs)
    {
        double total = 0;
        for (int t = 0; t < cfs.Length; t++)
            total += cfs[t] / Math.Pow(1 + rate, t);
        return total;
    }

    private static double Irr(double[] cfs)
    {
        double guess = 0.10;

        for (int i = 0; i < 50; i++)
        {
            var (f, df) = IrrFn(cfs, guess);
            if (Math.Abs(df) < 1e-12) break;

            var next = guess - f / df;
            if (!double.IsFinite(next)) break;

            if (Math.Abs(next - guess) < 1e-10) return next;
            guess = next;
        }

        double low = -0.99, high = 5.0;
        var fLow = IrrFn(cfs, low).f;
        var fHigh = IrrFn(cfs, high).f;

        if (fLow * fHigh > 0) return double.NaN;

        for (int i = 0; i < 100; i++)
        {
            var mid = (low + high) / 2;
            var fMid = IrrFn(cfs, mid).f;

            if (Math.Abs(fMid) < 1e-10) return mid;

            if (fLow * fMid < 0) { high = mid; }
            else { low = mid; }
        }

        return (low + high) / 2;
    }

    private static (double f, double df) IrrFn(double[] cfs, double r)
    {
        double f = 0, df = 0;
        for (int t = 0; t < cfs.Length; t++)
        {
            var denom = Math.Pow(1 + r, t);
            f += cfs[t] / denom;
            if (t > 0)
                df += -t * cfs[t] / (denom * (1 + r));
        }
        return (f, df);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    private static string ComputeCanonicalSha256(RentabilityInputDto input)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(input, JsonOptions));
        var canonical = CanonicalizeJson(doc.RootElement);

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(canonical);
        var hash = sha.ComputeHash(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string CanonicalizeJson(JsonElement el)
    {
        var sb = new StringBuilder();
        WriteCanonical(el, sb);
        return sb.ToString();
    }

    private static void WriteCanonical(JsonElement el, StringBuilder sb)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                sb.Append('{');
                var props = el.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal);
                var first = true;
                foreach (var p in props)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append('"').Append(p.Name).Append('"').Append(':');
                    WriteCanonical(p.Value, sb);
                }
                sb.Append('}');
                break;

            case JsonValueKind.Array:
                sb.Append('[');
                var af = true;
                foreach (var item in el.EnumerateArray())
                {
                    if (!af) sb.Append(',');
                    af = false;
                    WriteCanonical(item, sb);
                }
                sb.Append(']');
                break;

            case JsonValueKind.String:
                sb.Append(JsonSerializer.Serialize(el.GetString()));
                break;

            case JsonValueKind.Number:
                sb.Append(el.GetDecimal().ToString("0.################", CultureInfo.InvariantCulture));
                break;

            case JsonValueKind.True:
                sb.Append("true");
                break;

            case JsonValueKind.False:
                sb.Append("false");
                break;

            case JsonValueKind.Null:
                sb.Append("null");
                break;

            default:
                sb.Append("null");
                break;
        }
    }

    private static decimal Money(decimal v) => Round(v, 2);

    private static decimal ClampPct(decimal v) => Math.Clamp(v, -100m, 1000m);

    private static int ClampInt(int v, int min, int max) => Math.Clamp(v, min, max);

    private static decimal Round(decimal v, int digits) => Math.Round(v, digits, MidpointRounding.AwayFromZero);

    private static decimal Pow(decimal x, int n)
    {
        if (n <= 0) return 1;
        decimal r = 1;
        for (var i = 0; i < n; i++) r *= x;
        return r;
    }
}
